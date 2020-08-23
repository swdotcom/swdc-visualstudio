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
using System.Reflection;
using System.Windows.Forms;
using System.Threading.Tasks;
using EnvDTE80;

namespace SoftwareCo
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SoftwareCoPackage.PackageGuidString)]
    // [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(CodeMetricsToolPane),
        Window = Microsoft.VisualStudio.Shell.Interop.ToolWindowGuids.SolutionExplorer,
        MultiInstances=false)]
    public sealed class SoftwareCoPackage : AsyncPackage
    {
        #region fields
        /// <summary>
        /// SoftwareCoPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "0ae38c4e-1ac5-4457-bdca-bb2dfc342a1c";

        private DTEEvents _dteEvents;
        private DocumentEvents _docEvents;
        private TextDocumentKeyPressEvents _textDocKeyEvent;

        private System.Threading.Timer offlineDataTimer;

        // Used by Constants for version info
        public static DTE ObjDte;

        private SoftwareRepoManager _softwareRepoUtil;
        private SessionSummaryManager sessionSummaryMgr;
        private DocEventManager docEventMgr;

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        public static bool PLUGIN_READY = false;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SoftwareCoPackage"/> class.
        /// </summary>
        public SoftwareCoPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            Console.WriteLine("Initializing SoftwareCo");
        }


        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            Console.WriteLine("Initializing SoftwareCo - ASYNC");
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                base.Initialize();

                ObjDte = await GetServiceAsync(typeof(DTE)) as DTE;
                _dteEvents = ObjDte.Events.DTEEvents;

                Task.Delay(2000).ContinueWith((task) => { InitializeListenersAsync(); });
                
            }
            catch (Exception ex)
            {
                Logger.Error("Error in InitializeAsync", ex);
                
            }
           
        }

        public static string GetVersion()
        {
            return string.Format("{0}.{1}.{2}", CodeTimeAssembly.Version.Major, CodeTimeAssembly.Version.Minor, CodeTimeAssembly.Version.Build);
        }

        public static string GetOs()
        {
            return System.Environment.OSVersion.VersionString;
        }

        public async Task InitializeListenersAsync()
        {
            string MethodName = "InitializeListenersAsync";
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                string PluginVersion = GetVersion();
                Logger.Info(string.Format("Initializing Code Time v{0}", PluginVersion));
                Logger.FileLog("Initializing Code Time", MethodName);

                PackageManager.initialize(this, ObjDte);

                await this.InitializeUserInfoAsync();

                // VisualStudio Object
                Events2 events = (Events2)ObjDte.Events;
                _textDocKeyEvent = events.TextDocumentKeyPressEvents;
                _docEvents = events.DocumentEvents;

                SolutionEventOpenedAsync();

            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing SoftwareCo", ex);
            }
        }

        public async void SolutionEventOpenedAsync()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (!PLUGIN_READY)
            {
                string solutionDir = await PackageManager.GetSolutionDirectory();
                if (solutionDir == null || solutionDir.Equals(""))
                {
                    Task.Delay(3000).ContinueWith((task) =>
                    {
                        SolutionEventOpenedAsync();
                    });
                    return;
                }
                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;

                // init the session summary mgr
                sessionSummaryMgr = SessionSummaryManager.Instance;
                sessionSummaryMgr.InjectAsyncPackage(this);

                // update the latestPayloadTimestampEndUtc
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

                // init the wallclock
                WallclockManager wallclockMgr = WallclockManager.Instance;

                // setup event handlers
                _textDocKeyEvent.AfterKeyPress += docEventMgr.AfterKeyPressedAsync;
                _docEvents.DocumentOpened += docEventMgr.DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved += docEventMgr.DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += docEventMgr.DocEventsOnDocumentOpeningAsync;

                // init the code metrics tree mgr
                CodeMetricsTreeManager.Instance.InjectAsyncPackage(this);

                // initialize the menu commands
                await SoftwareLaunchCommand.InitializeAsync(this);
                await SoftwareDashboardLaunchCommand.InitializeAsync(this);
                await SoftwareTopFortyCommand.InitializeAsync(this);
                await SoftwareLoginCommand.InitializeAsync(this);
                await SoftwareToggleStatusInfoCommand.InitializeAsync(this);
                await SoftwareOpenCodeMetricsTreeCommand.InitializeAsync(this);

                if (_softwareRepoUtil == null)
                {
                    _softwareRepoUtil = new SoftwareRepoManager();
                }

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                offlineDataTimer = new System.Threading.Timer(
                      SendOfflineData,
                      null,
                      ONE_MINUTE,
                      ONE_MINUTE * 15);

                

                // make sure the last payload is in memory
                FileManager.GetLastSavedKeystrokeStats();

                // check if we've shown the readme or not
                bool initializedVisualStudioPlugin = FileManager.getItemAsBool("visualstudio_CtInit");
                if (!initializedVisualStudioPlugin)
                {
                    DashboardManager.Instance.LaunchReadmeFileAsync();
                    FileManager.setBoolItem("visualstudio_CtInit", true);

                    // launch the tree view
                    CodeMetricsTreeManager.Instance.OpenCodeMetricsPaneAsync();
                }

                string PluginVersion = GetVersion();
                Logger.Info(string.Format("Initialized Code Time v{0}", PluginVersion));

                ProcessKeystrokePayload(null);

                PLUGIN_READY = true;
            }
        }

        public void Dispose()
        {
            if (offlineDataTimer != null)
            {
                _textDocKeyEvent.AfterKeyPress -= docEventMgr.AfterKeyPressedAsync;
                _docEvents.DocumentOpened -= docEventMgr.DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing -= docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved -= docEventMgr.DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening -= docEventMgr.DocEventsOnDocumentOpeningAsync;

                offlineDataTimer.Dispose();
                offlineDataTimer = null;
            }
        }
        #endregion

        #region Event Handlers

        private void OnActiveWindow()
        {

        }
        #endregion

        #region Methods

        protected void ProcessKeystrokePayload(Object stateInfo)
        {
            // SoftwareCoUtil.SetTimeout(ONE_MINUTE, PostData, false);
            DocEventManager.Instance.PostData();
        }

        private async void ProcessRepoJobs(Object stateInfo)
        {
            try
            {
                SoftwareUserSession.SendHeartbeat("HOURLY");

                string dir = await PackageManager.GetSolutionDirectory();

                if (dir != null)
                {
                    _softwareRepoUtil.GetHistoricalCommitsAsync(dir);

                    _softwareRepoUtil.ProcessRepoMembers(dir);
                }
            }
            catch (Exception ex)
            {

                Logger.Error("ProcessHourlyJobs, error: " + ex.Message, ex);
            }
            
        }

        public static async void SendOfflineData(object stateinfo)
        {
            SendOfflinePluginBatchData();
        }

        public static async void SendOfflinePluginBatchData() {
            string MethodName = "SendOfflineData";
            Logger.Info(DateTime.Now.ToString());
            bool online = await SoftwareUserSession.IsOnlineAsync();

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
                string MethodName = "InitializeUserInfo";
                Logger.FileLog("Initializing User", MethodName);
                bool online = await SoftwareUserSession.IsOnlineAsync();
                bool softwareSessionFileExists = FileManager.softwareSessionFileExists();
                object jwt = FileManager.getItem("jwt");
                if (!softwareSessionFileExists || jwt == null || jwt.ToString().Equals(""))
                {
                    string result = await SoftwareUserSession.CreateAnonymousUserAsync(online);
                }

                // check if the "name" is set. if not, get the user
                string name = FileManager.getItemAsString("name");
                if (name == null || name.Equals(""))
                {
                    await SoftwareUserSession.IsLoggedOn(online);
                    SoftwareLoginCommand.UpdateEnabledState(true);
                    SoftwareLaunchCommand.UpdateEnabledState(true);
                }

                long sessionTresholdSeconds = FileManager.getItemAsLong("sessionThresholdInSec");
                if (sessionTresholdSeconds == 0)
                {
                    // update the session threshold in seconds config
                    FileManager.setNumericItem("sessionThresholdInSec", Constants.DEFAULT_SESSION_THRESHOLD_SECONDS);
                }

                if (online)
                {

                    // send heartbeat
                    SoftwareUserSession.SendHeartbeat("INITIALIZED");
                }

                // fetch the session summary
                WallclockManager.Instance.UpdateSessionSummaryFromServerAsync();

            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing UserInfo", ex);

            }
            
        }

        

        private async Task ShowOfflinePromptAsync()
        {
            string msg = "Our service is temporarily unavailable. We will try to reconnect again in 10 minutes. Your status bar will not update at this time.";
            string caption = "Code Time";
            MessageBoxButtons buttons = MessageBoxButtons.OK;

            // Displays the MessageBox.
            System.Windows.Forms.MessageBox.Show(msg, caption, buttons);
        }

        #endregion

        public static class CodeTimeAssembly
        {
            static readonly Assembly Reference = typeof(CodeTimeAssembly).Assembly;
           
            public static readonly Version Version = Reference.GetName().Version;
        }


    }
}
