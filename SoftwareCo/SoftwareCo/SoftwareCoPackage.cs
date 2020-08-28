using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;

namespace SoftwareCo
{

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CodeMetricsToolPane),
        Window = ToolWindowGuids.SolutionExplorer,
        MultiInstances = false)]
    public sealed class SoftwareCoPackage : AsyncPackage
    {
        #region fields

        public const string PackageGuidString = "0ae38c4e-1ac5-4457-bdca-bb2dfc342a1c";

        private Events2 events;
        private DocumentEvents _docEvents;
        private SelectionEvents _selectionEvents;
        private TextEditorEvents _textEditorEvents;
        private TextDocumentKeyPressEvents _textDocKeyEvents;

        private Timer offlineDataTimer;
        private Timer processPayloadTimer;

        // Used by Constants for version info
        public static DTE ObjDte;
        private DocEventManager docEventMgr;

        private static int ONE_MINUTE = 1000 * 60;
        public static bool INITIALIZED = false;

        public SoftwareCoPackage() { }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                base.Initialize();

                ObjDte = await GetServiceAsync(typeof(DTE)) as DTE;
                events = (Events2)ObjDte.Events;

                // Intialize the document event handlers
                _textEditorEvents = events.TextEditorEvents;
                _textDocKeyEvents = events.TextDocumentKeyPressEvents;
                _selectionEvents = events.SelectionEvents;
                _docEvents = events.DocumentEvents;

                string PluginVersion = EnvUtil.GetVersion();
                Logger.Info(string.Format("Initializing Code Time v{0}", PluginVersion));

                // init the package manager that will use the AsyncPackage to run main thread requests
                PackageManager.initialize(this, ObjDte);

                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;

                // setup event handlers
                _textDocKeyEvents.BeforeKeyPress += new _dispTextDocumentKeyPressEvents_BeforeKeyPressEventHandler(BeforeKeyPress);
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentOpening += docEventMgr.DocEventsOnDocumentOpeningAsync;
                _selectionEvents.OnChange += docEventMgr.OnChangeAsync;
                _textEditorEvents.LineChanged += docEventMgr.LineChangedAsync;

                // initialize the rest of the plugin lazily as it takes time to
                // select a new or existing project to open
                new Scheduler().Execute(() => CheckSolutionActivation(), 10000);
            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing Code Time", ex);
            }
        }

        public async void CheckSolutionActivation()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!INITIALIZED)
            {
                // don't initialize the rest of the plugin until a project is loaded
                string solutionDir = await PackageManager.GetSolutionDirectory();
                if (string.IsNullOrEmpty(solutionDir))
                {
                    // no solution, try again later
                    new Scheduler().Execute(() => CheckSolutionActivation(), 10000);
                } else
                {
                    // solution is activated, initialize
                    new Scheduler().Execute(() => InitializePlugin(), 5000);
                }
            }
        }

        private async void InitializePlugin() {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!INITIALIZED)
            {
                await this.InitializeUserInfoAsync();

                // initialize the tracker event manager
                TrackerEventManager.init();

                // update the latestPayloadTimestampEndUtc
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

                // init the wallclock
                WallclockManager wallclockMgr = WallclockManager.Instance;

                // initialize the menu commands
                SoftwareLaunchCommand.InitializeAsync(this);
                SoftwareDashboardLaunchCommand.InitializeAsync(this);
                SoftwareTopFortyCommand.InitializeAsync(this);
                SoftwareLoginCommand.InitializeAsync(this);
                SoftwareToggleStatusInfoCommand.InitializeAsync(this);
                SoftwareOpenCodeMetricsTreeCommand.InitializeAsync(this);

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                offlineDataTimer = new Timer(
                      SendOfflineData,
                      null,
                      ONE_MINUTE / 2,
                      ONE_MINUTE * 5);

                string PluginVersion = EnvUtil.GetVersion();
                Logger.Info(string.Format("Initialized Code Time v{0}", PluginVersion));

                new Scheduler().Execute(() => SendOfflinePluginBatchData(), 10000);

                new Scheduler().Execute(() => InitializeReadme(), 1000);

                INITIALIZED = true;
            }
        }

        private async void InitializeReadme()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            // check if we've shown the readme or not
            bool initializedVisualStudioPlugin = FileManager.getItemAsBool("visualstudio_CtInit");
            if (!initializedVisualStudioPlugin)
            {
                DashboardManager.Instance.LaunchReadmeFileAsync();
                FileManager.setBoolItem("visualstudio_CtInit", true);

                // launch the tree view
                PackageManager.OpenCodeMetricsPaneAsync();
            }
        }

        void BeforeKeyPress(string Keypress, EnvDTE.TextSelection Selection, bool InStatementCompletion, ref bool CancelKeypress)
        {
            docEventMgr.BeforeKeyPressAsync(Keypress, Selection, InStatementCompletion, CancelKeypress);
        }

        public void Dispose()
        {
            TrackerEventManager.TrackEditorActionEvent("editor", "deactivate");

            WallclockManager.Instance.Dispose();

            TrackerManager.Dispose();

            if (offlineDataTimer != null)
            {
                _textDocKeyEvents.BeforeKeyPress -= new _dispTextDocumentKeyPressEvents_BeforeKeyPressEventHandler(BeforeKeyPress);
                _docEvents.DocumentClosing -= docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentOpening -= docEventMgr.DocEventsOnDocumentOpeningAsync;
                _selectionEvents.OnChange -= docEventMgr.OnChangeAsync;
                _textEditorEvents.LineChanged -= docEventMgr.LineChangedAsync;

                offlineDataTimer.Dispose();
                offlineDataTimer = null;
            }
            if (processPayloadTimer != null)
            {
                processPayloadTimer.Dispose();
                processPayloadTimer = null;
            }

            INITIALIZED = false;
        }
        #endregion

        #region Methods

        public void ProcessKeystrokePayload(Object stateInfo)
        {
            DocEventManager.Instance.PostData();
        }

        public static async void SendOfflineData(object stateinfo)
        {
            SendOfflinePluginBatchData();
        }

        public static async void SendOfflinePluginBatchData()
        {

            Logger.Info(DateTime.Now.ToString());
            bool online = await SoftwareUserManager.IsOnlineAsync();

            if (!online)
            {
                return;
            }

            int batch_limit = 25;
            bool succeeded = false;
            List<string> offlinePluginData = FileManager.GetOfflinePayloadList();
            List<string> batchList = new List<string>();
            if (offlinePluginData != null && offlinePluginData.Count > 0)
            {

                for (int i = 0; i < offlinePluginData.Count; i++)
                {
                    string line = offlinePluginData[i];
                    if (i >= batch_limit)
                    {
                        // send this batch off
                        succeeded = await SendBatchData(batchList);
                        if (!succeeded)
                        {
                            if (offlinePluginData.Count > 1000)
                            {
                                // delete anyway, there's an issue and the data is gathering
                                File.Delete(FileManager.getSoftwareDataStoreFile());
                            }
                            return;
                        }
                    }
                    batchList.Add(line);
                }

                if (batchList.Count > 0)
                {
                    succeeded = await SendBatchData(batchList);
                }

                // delete the file
                if (succeeded)
                {
                    File.Delete(FileManager.getSoftwareDataStoreFile());
                }
                else if (offlinePluginData.Count > 1000)
                {
                    File.Delete(FileManager.getSoftwareDataStoreFile());
                }
            }

        }

        private static async Task<bool> SendBatchData(List<string> batchList)
        {
            // send this batch off
            string jsonData = "[" + string.Join(",", batchList) + "]";
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/batch", jsonData);
            if (!SoftwareHttpManager.IsOk(response) && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                // there was an error, don't delete the offline data
                return false;
            }
            batchList.Clear();
            return true;
        }

        private async Task InitializeUserInfoAsync()
        {
            try
            {
                bool softwareSessionFileExists = FileManager.softwareSessionFileExists();
                object jwt = FileManager.getItem("jwt");
                if (string.IsNullOrEmpty("jwt"))
                {
                    string result = await SoftwareUserManager.CreateAnonymousUserAsync();
                }

                // check if the "name" is set. if not, get the user
                string name = FileManager.getItemAsString("name");
                if (string.IsNullOrEmpty(name))
                {
                    await SoftwareUserManager.IsLoggedOn();
                    SoftwareLoginCommand.UpdateEnabledState(true);
                    SoftwareLaunchCommand.UpdateEnabledState(true);
                }

                long sessionTresholdSeconds = FileManager.getItemAsLong("sessionThresholdInSec");
                if (sessionTresholdSeconds == 0)
                {
                    // update the session threshold in seconds config
                    FileManager.setNumericItem("sessionThresholdInSec", Constants.DEFAULT_SESSION_THRESHOLD_SECONDS);
                }

                // fetch the session summary
                WallclockManager.Instance.UpdateSessionSummaryFromServerAsync();

            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing UserInfo", ex);

            }

        }

        #endregion
    }
}
