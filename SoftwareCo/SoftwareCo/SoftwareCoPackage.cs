using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;

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
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.BackgroundLoad)]
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
        private WindowEvents _windowEvents;
        private TextDocumentKeyPressEvents _textDocKeyEvent;

        private System.Threading.Timer timer;
        private System.Threading.Timer kpmTimer;
        private System.Threading.Timer repoCommitsTimer;
        private System.Threading.Timer musicTimer;
        private System.Threading.Timer statusMsgTimer;
        private System.Threading.Timer userStatusTimer;
        private System.Threading.Timer offlineDataTimer;

        // Used by Constants for version info
        public static DTE2 ObjDte;

        // this is the solution full name
        private string _solutionName = string.Empty;

        private SoftwareData _softwareData;
        
        private SoftwareRepoManager _softwareRepoUtil;
        private SessionSummaryManager sessionSummaryMgr;
        private DocEventManager docEventMgr;

        private static SoftwareStatus _softwareStatus;

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ONE_HOUR = ONE_MINUTE * 60;
        private static int THIRTY_MINUTES = ONE_MINUTE * 30;

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
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                base.Initialize();

                ObjDte = await GetServiceAsync(typeof(DTE)) as DTE2;
                _dteEvents = ObjDte.Events.DTEEvents;
                _dteEvents.OnStartupComplete += OnOnStartupComplete;

                InitializeListenersAsync();
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
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
                string PluginVersion = GetVersion();
                Logger.Info(string.Format("Initializing Code Time v{0}", PluginVersion));
                Logger.FileLog("Initializing Code Time", MethodName);
                // VisualStudio Object
                Events2 events = (Events2)ObjDte.Events;
                _textDocKeyEvent = events.TextDocumentKeyPressEvents;
                _docEvents = ObjDte.Events.DocumentEvents;

                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;
                DocEventManager.ObjDte = ObjDte;
                // init the session summary mgr
                sessionSummaryMgr = SessionSummaryManager.Instance;
                // init the code metrics tree mgr
                CodeMetricsTreeManager.Instance.InjectAsyncPackage(this);

                // setup event handlers
                _textDocKeyEvent.AfterKeyPress += docEventMgr.AfterKeyPressedAsync;
                _docEvents.DocumentOpened += docEventMgr.DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved += docEventMgr.DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += docEventMgr.DocEventsOnDocumentOpeningAsync;

                //initialize the StatusBar 
                await InitializeSoftwareStatusAsync();

                // initialize the menu commands
                await SoftwareLaunchCommand.InitializeAsync(this);
                await SoftwareDashboardLaunchCommand.InitializeAsync(this);
                await SoftwareTopFortyCommand.InitializeAsync(this);
                await SoftwareLoginCommand.InitializeAsync(this);
                await SoftwareToggleStatusInfoCommand.InitializeAsync(this);

                if (_softwareRepoUtil == null)
                {
                    _softwareRepoUtil = new SoftwareRepoManager();
                }
                InitializeStatusBarButton();
                sessionSummaryMgr.UpdateStatusBarWithSummaryData();

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                offlineDataTimer = new System.Threading.Timer(
                      SendOfflineData,
                      null,
                      THIRTY_MINUTES,
                      THIRTY_MINUTES);


                int delay = 1000 * 45;

                delay = ONE_MINUTE + (1000 * 10);
                repoCommitsTimer = new System.Threading.Timer(
                    ProcessHourlyJobs,
                    autoEvent,
                    delay,
                    ONE_HOUR);

                statusMsgTimer = new System.Threading.Timer(
                    UpdateStatusMsg,
                    autoEvent,
                    1000 * 30,
                    1000 * 10);

                userStatusTimer = new System.Threading.Timer(
                    UpdateUserStatus,
                    autoEvent,
                    ONE_MINUTE,
                    1000 * 120);

                this.InitializeUserInfo();
            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing SoftwareCo", ex);
            }
        }

        private void InitializeStatusBarButton()
        {
            SoftwareCoUtil.GetStatusBar(false);
        }

        private async Task InitializeSoftwareStatusAsync()
        {
            if (_softwareStatus == null)
            {
                IVsStatusbar statusbar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                _softwareStatus = new SoftwareStatus(statusbar);
            }
        }
        public void Dispose()
        {
            if (timer != null)
            {
                _textDocKeyEvent.AfterKeyPress -= docEventMgr.AfterKeyPressedAsync;
                _docEvents.DocumentOpened -= docEventMgr.DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing -= docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved -= docEventMgr.DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening -= docEventMgr.DocEventsOnDocumentOpeningAsync;

                timer.Dispose();
                timer = null;
            }
        }
        #endregion

        #region Event Handlers

        private void OnOnStartupComplete()
        {
            //
        }
        #endregion

        #region Methods

        public static void ToggleStatusInfo()
        {
            if (_softwareStatus != null)
            {
                _softwareStatus.ToggleStatusInfo();

            }
        }

        private void ProcessHourlyJobs(Object stateInfo)
        {
            try
            {
                SoftwareUserSession.SendHeartbeat("HOURLY");

                string dir = GetSolutionDirectory();

                if (dir != null)
                {
                    _softwareRepoUtil.GetHistoricalCommitsAsync(dir);
                }
            }
            catch (Exception ex)
            {

                Logger.Error("ProcessHourlyJobs, error: " + ex.Message, ex);
            }
            
        }

        private async void LaunchLoginPrompt()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            bool online = await SoftwareUserSession.IsOnlineAsync();

            if (online)
            {
                string msg = "Finish creating your account and see rich data visualizations.";
                const string caption = "Code Time";
                DialogResult result = MessageBox.Show(msg, caption, MessageBoxButtons.OKCancel);

                // If the no button was pressed ...
                if (result == DialogResult.Yes)
                {
                    // launch the browser
                    SoftwareCoUtil.launchLogin();
                }
            }
        }

        private async void SendOfflineData(object stateinfo)
        {
            string MethodName = "SendOfflineData";
            Logger.Info(DateTime.Now.ToString());
            bool online = await SoftwareUserSession.IsOnlineAsync();

            if (!online)
            {
                return;
            }

            string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
            if (File.Exists(datastoreFile))
            {
                // get the content
                string[] lines = File.ReadAllLines(datastoreFile, System.Text.Encoding.UTF8);

                if (lines != null && lines.Length > 0)
                {
                    List<String> jsonLines = new List<string>();
                    foreach (string line in lines)
                    {
                        if (line != null && line.Trim().Length > 0)
                        {
                            jsonLines.Add(line);
                        }
                    }
                    string jsonContent = "[" + string.Join(",", jsonLines) + "]";
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/batch", jsonContent);
                    if (SoftwareHttpManager.IsOk(response))
                    {
                        // delete the file
                        File.Delete(datastoreFile);
                    }
                }
            }

            sessionSummaryMgr.ÇlearSessionSummaryData();
        }


        private string GetSolutionDirectory()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
            {
                return Path.GetDirectoryName(ObjDte.Solution.FileName);
            }
            return null;
        }

        private async void InitializeUserInfo()
        {
            try
            {
                string MethodName = "InitializeUserInfo";
                Logger.FileLog("Initializing User", MethodName);
                bool online = await SoftwareUserSession.IsOnlineAsync();
                bool softwareSessionFileExists = SoftwareCoUtil.softwareSessionFileExists();
                bool jwtExists = SoftwareCoUtil.jwtExists();
                bool initializedUser = false;
                if (!softwareSessionFileExists || !jwtExists)
                {
                    string result = await SoftwareUserSession.CreateAnonymousUserAsync(online);
                    if (result != null)
                    {
                        initializedUser = true;
                    }
                }

                SoftwareUserSession.UserStatus status = await SoftwareUserSession.GetUserStatusAsync(true);

                SoftwareLoginCommand.UpdateEnabledState(status);

                if (initializedUser)
                {
                    LaunchLoginPrompt();
                }

                if (online)
                {

                    // send heartbeat
                    SoftwareUserSession.SendHeartbeat("INITIALIZED");
                }

            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing UserInfo", ex);

            }
            
        }

        private String getDownloadDestinationDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        public void UpdateStatusMsg(Object stateInfo)
        {
            _softwareStatus.ReloadStatus();
        }

        public async void UpdateUserStatus(Object stateInfo)
        {
            SoftwareUserSession.UserStatus status = await SoftwareUserSession.GetUserStatusAsync(false);
        }

        public async Task OpenCodeMetricsPane()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            ToolWindowPane window = this.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        private async Task ShowOfflinePromptAsync()
        {

            string msg = "Our service is temporarily unavailable. We will try to reconnect again in 10 minutes. Your status bar will not update at this time.";
            string caption = "Code Time";
            MessageBoxButtons buttons = MessageBoxButtons.OK;

            // Displays the MessageBox.
            MessageBox.Show(msg, caption, buttons);
        }

        #endregion

        public static class CodeTimeAssembly
        {
            static readonly Assembly Reference = typeof(CodeTimeAssembly).Assembly;
            public static readonly Version Version = Reference.GetName().Version;
        }
    }
}
