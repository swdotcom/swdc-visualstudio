using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace SoftwareCo
{

    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
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
        private TextEditorEvents _textEditorEvents;
        private TextDocumentKeyPressEvents _textDocKeyEvents;
        private WindowVisibilityEvents _windowVisibilityEvents;

        // Used by Constants for version info
        public static DTE ObjDte;
        private DocEventManager docEventMgr;

        public static bool INITIALIZED = false;

        private int solutionTryThreshold = 6;
        private int solutionTryCount = 0;

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

                // create the anon user if it's a new install (async)
                await InitializeUserInfoAsync();

                // obtain the DTE service to track doc changes
                ObjDte = await GetServiceAsync(typeof(DTE)) as DTE;
                events = (Events2)ObjDte.Events;

                // Intialize the document event handlers
                _textEditorEvents = events.TextEditorEvents;
                _textDocKeyEvents = events.TextDocumentKeyPressEvents;
                _docEvents = events.DocumentEvents;
                _windowVisibilityEvents = events.WindowVisibilityEvents;

                // init the package manager that will use the AsyncPackage to run main thread requests
                PackageManager.initialize(this, ObjDte);

                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;

                // setup event handlers
                _textDocKeyEvents.BeforeKeyPress += this.BeforeKeyPress;
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _windowVisibilityEvents.WindowShowing += docEventMgr.WindowVisibilityEventAsync;
                _textEditorEvents.LineChanged += docEventMgr.LineChangedAsync;

                // initialize the rest of the plugin in 10 seconds (allow the user to select a solution to open)
                System.Threading.Tasks.Task.Delay(10000).ContinueWith((task) => { CheckSolutionActivation(); });
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
                if (string.IsNullOrEmpty(solutionDir) || solutionTryCount > solutionTryThreshold)
                {
                    solutionTryCount++;
                    // no solution, try again later
                    _ = System.Threading.Tasks.Task.Delay(5000).ContinueWith((task) => { CheckSolutionActivation(); });
                }
                else
                {
                    // solution is activated or it's empty, initialize
                    _ = System.Threading.Tasks.Task.Delay(1000).ContinueWith((task) => { InitializePlugin(); });
                }
            }
        }

        private void InitializePlugin()
        {
            // update the latestPayloadTimestampEndUtc
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

            // initialize the menu commands
            SoftwareLaunchCommand.InitializeAsync(this);
            SoftwareDashboardLaunchCommand.InitializeAsync(this);
            SoftwareLoginCommand.InitializeAsync(this);
            SoftwareToggleStatusInfoCommand.InitializeAsync(this);
            SoftwareOpenCodeMetricsTreeCommand.InitializeAsync(this);

            // check if the "name" is set. if not, get the user
            string name = FileManager.getItemAsString("name");
            if (string.IsNullOrEmpty(name))
            {
                // enable the login command
                SoftwareLoginCommand.UpdateEnabledState(true);
            }
            else
            {
                // enable web dashboard command
                SoftwareLaunchCommand.UpdateEnabledState(true);
            }

            Logger.Info(string.Format("Initialized Code Time v{0}", EnvUtil.GetVersion()));

            // initialize the tracker event manager
            TrackerEventManager.init();

            INITIALIZED = true;

            InitializeStatusBarAndWallClock();

            // show the readme if it's the initial install
            InitializeReadme();
        }

        private async void InitializeTracker()
        {
            // initialize the tracker event manager
            TrackerEventManager.init();
        }

        private async void InitializeStatusBarAndWallClock()
        {
            PackageManager.InitializeStatusBar();

            // this needs INITIALIZED to be true at this point
            WallclockManager.Initialize();
        }

        private async void InitializeReadme()
        {
            try
            {
                // check if we've shown the readme or not
                bool initializedVisualStudioPlugin = FileManager.getItemAsBool("visualstudio_CtInit");
                if (!initializedVisualStudioPlugin)
                {
                    DashboardManager.Instance.LaunchReadmeFileAsync();
                    FileManager.setBoolItem("visualstudio_CtInit", true);

                    // launch the tree view
                    PackageManager.OpenCodeMetricsPaneAsync();
                }
            } catch (Exception) { }
        }

        void BeforeKeyPress(string Keypress, TextSelection Selection, bool InStatementCompletion, ref bool CancelKeypress)
        {
            docEventMgr.BeforeKeyPressAsync(Keypress, Selection, InStatementCompletion, CancelKeypress);
        }

        public void Dispose()
        {
            TrackerEventManager.TrackEditorActionEvent("editor", "deactivate");

            WallclockManager.Dispose();

            TrackerManager.Dispose();

            _textDocKeyEvents.BeforeKeyPress -= this.BeforeKeyPress;
            _docEvents.DocumentClosing -= docEventMgr.DocEventsOnDocumentClosedAsync;
            _textEditorEvents.LineChanged -= docEventMgr.LineChangedAsync;
            _windowVisibilityEvents.WindowShowing -= docEventMgr.WindowVisibilityEventAsync;

            INITIALIZED = false;
        }
        #endregion

        #region Methods

        public void ProcessKeystrokePayload(Object stateInfo)
        {
            DocEventManager.Instance.PostData();
        }

        private async Task InitializeUserInfoAsync()
        {
            try
            {
                bool softwareSessionFileExists = FileManager.softwareSessionFileExists();
                string jwt = FileManager.getItemAsString("jwt");
                if (string.IsNullOrEmpty(jwt))
                {
                    SoftwareUserManager.CreateAnonymousUserAsync(false);
                }

                long sessionTresholdSeconds = FileManager.getItemAsLong("sessionThresholdInSec");
                if (sessionTresholdSeconds == 0)
                {
                    // update the session threshold in seconds config
                    FileManager.setNumericItem("sessionThresholdInSec", Constants.DEFAULT_SESSION_THRESHOLD_SECONDS);
                }

            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing UserInfo", ex);

            }

        }

        #endregion
    }
}
