using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using EnvDTE80;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    // [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CodeMetricsToolPane),
        Window = ToolWindowGuids.SolutionExplorer,
        MultiInstances=false)]
    public sealed class SoftwareCoPackage : AsyncPackage
    {
        #region fields

        public const string PackageGuidString = "0ae38c4e-1ac5-4457-bdca-bb2dfc342a1c";

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
        public static bool PLUGIN_READY = false;

        public SoftwareCoPackage() {}

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

                string PluginVersion = EnvUtil.GetVersion();
                Logger.Info(string.Format("Initializing Code Time v{0}", PluginVersion));

                // init the package manager that will use the AsyncPackage to run main thread requests
                PackageManager.initialize(this, ObjDte);

                // Intialize the document event handlers
                Events2 events = (Events2)ObjDte.Events;
                _textEditorEvents = events.TextEditorEvents;
                _textDocKeyEvents = events.TextDocumentKeyPressEvents;
                _selectionEvents = events.SelectionEvents;
                _docEvents = events.DocumentEvents;

                InitializePlugin();
            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing SoftwareCo", ex);
            }
        }

        public async void InitializePlugin()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!PLUGIN_READY)
            {
                string solutionDir = await PackageManager.GetSolutionDirectory();
                if (string.IsNullOrEmpty(solutionDir))
                {
                    Task.Delay(3000).ContinueWith((task) =>
                    {
                        InitializePlugin();
                    });
                    return;
                }
                await this.InitializeUserInfoAsync();

                // initialize the tracker event manager
                TrackerEventManager.init();

                // update the latestPayloadTimestampEndUtc
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

                // init the wallclock
                WallclockManager wallclockMgr = WallclockManager.Instance;

                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;

                // setup event handlers
                _textDocKeyEvents.BeforeKeyPress += new _dispTextDocumentKeyPressEvents_BeforeKeyPressEventHandler(BeforeKeyPress);
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentOpening += docEventMgr.DocEventsOnDocumentOpeningAsync;
                _selectionEvents.OnChange += docEventMgr.OnChangeAsync;
                _textEditorEvents.LineChanged += docEventMgr.LineChangedAsync;


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
                      ONE_MINUTE * 15);

                processPayloadTimer = new Timer(
                    ProcessKeystrokePayload,
                    null,
                    ONE_MINUTE,
                    ONE_MINUTE);

                // check if we've shown the readme or not
                bool initializedVisualStudioPlugin = FileManager.getItemAsBool("visualstudio_CtInit");
                if (!initializedVisualStudioPlugin)
                {
                    DashboardManager.Instance.LaunchReadmeFileAsync();
                    FileManager.setBoolItem("visualstudio_CtInit", true);

                    // launch the tree view
                    PackageManager.OpenCodeMetricsPaneAsync();
                }

                string PluginVersion = EnvUtil.GetVersion();
                Logger.Info(string.Format("Initialized Code Time v{0}", PluginVersion));

                Task.Delay(5000).ContinueWith((task) => { ProcessKeystrokePayload(null); });
                Task.Delay(8000).ContinueWith((task) => { FileManager.GetLastSavedKeystrokeStats(); });

                PLUGIN_READY = true;
            }
        }

        void BeforeKeyPress(string Keypress, EnvDTE.TextSelection Selection, bool InStatementCompletion, ref bool CancelKeypress)
        {
            docEventMgr.BeforeKeyPressAsync(Keypress, Selection, InStatementCompletion, CancelKeypress);
        }

        public void Dispose()
        {
            TrackerEventManager.TrackEditorActionEvent("editor", "deactivate");

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

        public static async void SendOfflinePluginBatchData() {

            Logger.Info(DateTime.Now.ToString());
            bool online = await SoftwareUserManager.IsOnlineAsync();

            if (!online)
            {
                return;
            }

            int batch_limit = 5;
            HttpResponseMessage response = null;
            string jsonData = "";
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
                        jsonData = "[" + string.Join(",", batchList) + "]";
                        response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/batch", jsonData);
                        if (!SoftwareHttpManager.IsOk(response))
                        {
                            // there was an error, don't delete the offline data
                            return;
                        }
                        batchList.Clear();
                    }
                    batchList.Add(line);
                }

                if (batchList.Count > 0)
                {
                    jsonData = "[" + string.Join(",", batchList) + "]";
                    response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/batch", jsonData);
                    if (!SoftwareHttpManager.IsOk(response))
                    {
                        // there was an error, don't delete the offline data
                        return;
                    }
                }

                // delete the file
                File.Delete(FileManager.getSoftwareDataStoreFile());
            }
            
        }

        private async Task InitializeUserInfoAsync()
        {
            try
            {
                bool online = await SoftwareUserManager.IsOnlineAsync();
                bool softwareSessionFileExists = FileManager.softwareSessionFileExists();
                object jwt = FileManager.getItem("jwt");
                if (!softwareSessionFileExists || jwt == null || jwt.ToString().Equals(""))
                {
                    string result = await SoftwareUserManager.CreateAnonymousUserAsync(online);
                }

                // check if the "name" is set. if not, get the user
                string name = FileManager.getItemAsString("name");
                if (name == null || name.Equals(""))
                {
                    await SoftwareUserManager.IsLoggedOn(online);
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
