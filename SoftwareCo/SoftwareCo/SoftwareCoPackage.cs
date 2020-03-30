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
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

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
        private TextDocumentKeyPressEvents _textDocKeyEvent;

        private System.Threading.Timer timer;
        private System.Threading.Timer repoCommitsTimer;
        private System.Threading.Timer offlineDataTimer;

        // Used by Constants for version info
        public static DTE2 ObjDte;

        private StatusBarButton _statusBarButton;
        private bool _addedStatusBarButton = false;

        private CodeMetricsToolPane _codeMetricsWindow;

        private SoftwareRepoManager _softwareRepoUtil;
        private SessionSummaryManager sessionSummaryMgr;
        private DocEventManager docEventMgr;

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ONE_HOUR = ONE_MINUTE * 60;
        private static int THIRTY_MINUTES = ONE_MINUTE * 30;
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
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                base.Initialize();

                ObjDte = await GetServiceAsync(typeof(DTE)) as DTE2;
                _dteEvents = ObjDte.Events.DTEEvents;

                Task.Delay(1000).ContinueWith((task) => { InitializeListenersAsync(); });
                
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

                _statusBarButton = new StatusBarButton();

                await this.InitializeUserInfoAsync();

                // VisualStudio Object
                Events2 events = (Events2)ObjDte.Events;
                _textDocKeyEvent = events.TextDocumentKeyPressEvents;
                _docEvents = events.DocumentEvents;

                // init the doc event mgr and inject ObjDte
                docEventMgr = DocEventManager.Instance;
                DocEventManager.ObjDte = ObjDte;

                // init the session summary mgr
                sessionSummaryMgr = SessionSummaryManager.Instance;
                sessionSummaryMgr.InjectAsyncPackage(this);

                // init the GitUtilManager
                GitUtilManager.Instance.InjectAsyncPackage(this);

                // init the code metrics tree mgr
                CodeMetricsTreeManager.Instance.InjectAsyncPackage(this);

                // init the event manager and inject this
                EventManager.Instance.InjectAsyncPackage(this);

                // init the wallclock
                WallclockManager wallclockMgr = WallclockManager.Instance;
                wallclockMgr.InjectAsyncPackage(this, ObjDte);

                // setup event handlers
                _textDocKeyEvent.AfterKeyPress += docEventMgr.AfterKeyPressedAsync;
                _docEvents.DocumentOpened += docEventMgr.DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing += docEventMgr.DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved += docEventMgr.DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += docEventMgr.DocEventsOnDocumentOpeningAsync;

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

                repoCommitsTimer = new System.Threading.Timer(
                    ProcessRepoJobs,
                    autoEvent,
                    ONE_MINUTE * 5,
                    ONE_MINUTE * 20);

                // update the session summary global and averages for the new day
                // rebuild the code metrics data in the tree
                this.RebuildCodeMetricsAsync();

                // update the git metrics
                this.RebuildGitMetricsAsync();

                // initialize the status bar before we fetch the summary data
                InitializeStatusBar();

                // fetch the session summary
                wallclockMgr.UpdateSessionSummaryFromServerAsync(false);

                // check if we've shown the readme or not
                bool initializedVisualStudioPlugin = SoftwareCoUtil.getItemAsBool("visualstudio_CtInit");
                if (!initializedVisualStudioPlugin)
                {
                    DashboardManager.Instance.LaunchReadmeFileAsync();
                    SoftwareCoUtil.setBoolItem("visualstudio_CtInit", true);

                    // launch the tree view
                    CodeMetricsTreeManager.Instance.OpenCodeMetricsPaneAsync();
                }

                Task.Delay(3000).ContinueWith((task) => {
                    EventManager.Instance.CreateCodeTimeEvent("resource", "load", "EditorActivate");
                });

                PLUGIN_READY = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Error Initializing SoftwareCo", ex);
            }
        }

        public void Dispose()
        {
            EventManager.Instance.CreateCodeTimeEvent("resource", "unload", "EditorDeactivate");
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

        private void OnActiveWindow()
        {

        }
        #endregion

        #region Methods

        private void ProcessRepoJobs(Object stateInfo)
        {
            try
            {
                SoftwareUserSession.SendHeartbeat("HOURLY");

                string dir = DocEventManager._solutionDirectory;

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
            SendOfflinePluginBatchData(false);
        }

        public static async void SendOfflinePluginBatchData(bool isNewDay) {
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

            // update the session summary global and averages for the new day
            Task.Delay(ONE_MINUTE).ContinueWith((task) => { WallclockManager.Instance.UpdateSessionSummaryFromServerAsync(isNewDay); });
        }

        private async Task InitializeUserInfoAsync()
        {
            try
            {
                string MethodName = "InitializeUserInfo";
                Logger.FileLog("Initializing User", MethodName);
                bool online = await SoftwareUserSession.IsOnlineAsync();
                bool softwareSessionFileExists = SoftwareCoUtil.softwareSessionFileExists();
                object jwt = SoftwareCoUtil.getItem("jwt");
                if (!softwareSessionFileExists || jwt == null || jwt.ToString().Equals(""))
                {
                    string result = await SoftwareUserSession.CreateAnonymousUserAsync(online);
                }

                // check if the "name" is set. if not, get the user
                string name = SoftwareCoUtil.getItemAsString("name");
                if (name == null || name.Equals(""))
                {
                    await SoftwareUserSession.IsLoggedOn(online);
                    SoftwareLoginCommand.UpdateEnabledState(true);
                    SoftwareLaunchCommand.UpdateEnabledState(true);
                }

                long sessionTresholdSeconds = SoftwareCoUtil.getItemAsLong("sessionThresholdInSec");
                if (sessionTresholdSeconds == 0)
                {
                    // update the session threshold in seconds config
                    SoftwareCoUtil.setNumericItem("sessionThresholdInSec", Constants.DEFAULT_SESSION_THRESHOLD_SECONDS);
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

        public async Task UpdateStatusBarButtonText(String text, String iconName = null)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await InitializeStatusBar();

            if (!EventManager.Instance.IsShowingStatusText())
            {
                text = "";
                iconName = "clock.png";
            }

            if (iconName == null || iconName.Equals(""))
            {
                iconName = "cpaw.png";
            }

            _statusBarButton.UpdateDisplayAsync(text, iconName);
        }

        public async Task InitializeStatusBar()
        {
            if (_addedStatusBarButton)
            {
                return;
            }
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            DockPanel statusBarObj = FindChildControl<DockPanel>(System.Windows.Application.Current.MainWindow, "StatusBarPanel");
            if (statusBarObj != null)
            {
                statusBarObj.Children.Insert(0, _statusBarButton);
                _addedStatusBarButton = true;
            }
        }

        public T FindChildControl<T>(DependencyObject parent, string childName)
          where T : DependencyObject
        {
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                T childType = child as T;
                if (childType == null)
                {

                    foundChild = FindChildControl<T>(child, childName);


                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {

                    if (child is FrameworkElement frameworkElement && frameworkElement.Name == childName)
                    {

                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        public async Task RebuildMenuButtonsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildMenuButtons();
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)this.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildMenuButtons();
        }

        public async Task RebuildCodeMetricsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildCodeMetrics();
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)this.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildCodeMetrics();
        }

        public async Task RebuildGitMetricsAsync()
        {
            if (_codeMetricsWindow != null && _codeMetricsWindow.Frame != null)
            {
                _codeMetricsWindow.RebuildGitMetricsAsync();
            }

            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _codeMetricsWindow = (CodeMetricsToolPane)this.FindToolWindow(typeof(CodeMetricsToolPane), 0, true);
            if ((null == _codeMetricsWindow) || (null == _codeMetricsWindow.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }
            _codeMetricsWindow.RebuildGitMetricsAsync();
        }

        public async Task OpenCodeMetricsPaneAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
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
