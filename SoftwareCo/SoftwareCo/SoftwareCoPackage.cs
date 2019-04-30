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
using Thread = System.Threading.Thread;

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

        // Used by Constants for version info
        public static DTE2 ObjDte;

        // this is the solution full name
        private string _solutionName = string.Empty;
        private int postFrequency = 1; // every minute

        private DateTime _lastPostTime = DateTime.UtcNow;
        private SoftwareData _softwareData;
        private SoftwareRepoManager _softwareRepoUtil;
        private static SoftwareStatus _softwareStatus;

        private bool _isOnline = true;
        private bool _isAuthenticated = true;
        private bool _hasJwt = true;

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ONE_HOUR = ONE_MINUTE * 60;

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
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            base.Initialize();

            ObjDte = await GetServiceAsync(typeof(DTE)) as DTE2;
            _dteEvents = ObjDte.Events.DTEEvents;
            _dteEvents.OnStartupComplete += OnOnStartupComplete;

            InitializeListenersAsync();
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
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            try
            {
                string PluginVersion = GetVersion();
                Logger.Info(string.Format("Initializing Code Time v{0}", PluginVersion));

                // VisualStudio Object
                Events2 events = (Events2)ObjDte.Events;
                _textDocKeyEvent = events.TextDocumentKeyPressEvents;
                _docEvents = ObjDte.Events.DocumentEvents;

                // setup event handlers
                _textDocKeyEvent.AfterKeyPress += AfterKeyPressed;
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentClosing += DocEventsOnDocumentClosed;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += DocEventsOnDocumentOpening;

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

                if (_softwareStatus == null)
                {
                    IVsStatusbar statusbar = await GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                    _softwareStatus = new SoftwareStatus(statusbar);
                }

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                // setup timer to process events every 1 minute
                timer = new System.Threading.Timer(
                    ProcessSoftwareDataTimerCallbackAsync,
                    autoEvent,
                    ONE_MINUTE,
                    ONE_MINUTE);

                this.SendOfflineData();

                // start in 5 seconds every 5 min
                int delay = 1000 * 5;
                kpmTimer = new System.Threading.Timer(
                    ProcessFetchDailyKpmTimerCallbackAsync,
                    autoEvent,
                    delay,
                    ONE_MINUTE * 5);

                delay = 1000 * 45;

                delay = ONE_MINUTE + (1000 * 10);
                repoCommitsTimer = new System.Threading.Timer(
                    ProcessHourlyJobs,
                    autoEvent,
                    delay,
                    ONE_HOUR);

                musicTimer = new System.Threading.Timer(
                    ProcessMusicTracksAsync,
                    autoEvent,
                    1000 * 5,
                    1000 * 30);

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

        public void Dispose()
        {
            if (timer != null)
            {
                _textDocKeyEvent.AfterKeyPress -= AfterKeyPressed;
                _docEvents.DocumentOpened -= DocEventsOnDocumentOpened;
                _docEvents.DocumentClosing -= DocEventsOnDocumentClosed;
                _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening -= DocEventsOnDocumentOpening;

                timer.Dispose();
                timer = null;

                // process any remaining data
                ProcessSoftwareDataTimerCallbackAsync(null);
            }
        }
        #endregion

        #region Event Handlers

        private void DocEventsOnDocumentSaved(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (_softwareData == null || !_softwareData.source.ContainsKey(fileName))
            {
                return;
            }

            InitializeSoftwareData(fileName);

            FileInfo fi = new FileInfo(fileName);

            _softwareData.UpdateData(fileName, "length", fi.Length);

            try
            {
                _softwareStatus.ReloadStatus();
            }
            catch (ThreadInterruptedException e)
            {
                //
            }
        }

        private void DocEventsOnDocumentOpening(String docPath, Boolean readOnly)
        {
            FileInfo fi = new FileInfo(docPath);
            String fileName = fi.FullName;
            InitializeSoftwareData(fileName);
        }

        private void AfterKeyPressed(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
            String fileName = ObjDte.ActiveWindow.Document.FullName;
            InitializeSoftwareData(fileName);

            if (ObjDte.ActiveWindow.Document.Language != null)
            {
                _softwareData.addOrUpdateFileStringInfo(fileName, "syntax", ObjDte.ActiveWindow.Document.Language);
            }
            if (!String.IsNullOrEmpty(Keypress))
            {
                FileInfo fi = new FileInfo(fileName);

                bool isNewLine = false;
                if (Keypress == "\b")
                {
                    // register a delete event
                    _softwareData.UpdateData(fileName, "delete", 1);
                    Logger.Info("Code Time: Delete character incremented");
                }
                else if (Keypress == "\r")
                {
                    isNewLine = true;
                }
                else
                {
                    _softwareData.UpdateData(fileName, "add", 1);
                    Logger.Info("Code Time: KPM incremented");
                }

                if (isNewLine)
                {
                    _softwareData.addOrUpdateFileInfo(fileName, "linesAdded", 1);
                }

                _softwareData.keystrokes += 1;
            }
        }

        private void DocEventsOnDocumentOpened(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (_softwareData == null || !_softwareData.source.ContainsKey(fileName))
            {
                return;
            }
            try
            {
                _softwareData.UpdateData(fileName, "open", 1);
                Logger.Info("Code Time: File open incremented");
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentClosed(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (_softwareData == null || !_softwareData.source.ContainsKey(fileName))
            {
                return;
            }
            try
            {
                _softwareData.UpdateData(fileName, "close", 1);
                Logger.Info("Code Time: File close incremented");
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentClosed", ex);
            }
        }

        private void OnOnStartupComplete()
        {
            //
        }
        #endregion

        #region Methods

        public static void ToggleStatusInfo()
        {
            _softwareStatus.ToggleStatusInfo();
        }

        private void ProcessHourlyJobs(Object stateInfo)
        {
            SoftwareUserSession.SendHeartbeat("HOURLY");

            string dir = getSolutionDirectory();

            if (dir != null)
            {
                _softwareRepoUtil.GetHistoricalCommitsAsync(dir);
            }
        }

        private async void ProcessMusicTracksAsync(Object stateInfo)
        {
            try
            {
                await SoftwareSpotifyManager.GetLocalSpotifyTrackInfoAsync();
            } catch (Exception e)
            {
                Logger.Error("Unable to get spotify track info, error: " + e.Message);
            }
        }

        // This method is called by the timer delegate.
        private async void ProcessSoftwareDataTimerCallbackAsync(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            this.SendOfflineData();

            DateTime now = DateTime.UtcNow;
            if (_softwareData != null && _softwareData.HasData() && (EnoughTimePassed(now) || timer == null))
            {
                double offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
                _softwareData.local_start = _softwareData.start + ((int)offset * 60);
                _softwareData.offset = Math.Abs((int)offset);
                if (TimeZone.CurrentTimeZone.DaylightName != null
                    && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
                {
                    _softwareData.timezone = TimeZone.CurrentTimeZone.DaylightName;
                }
                else
                {
                    _softwareData.timezone = TimeZone.CurrentTimeZone.StandardName;
                }

                string softwareDataContent = _softwareData.GetAsJson();
                Logger.Info("Code Time: sending: " + softwareDataContent);

                if (SoftwareCoUtil.isTelemetryOn())
                {

                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data", softwareDataContent);

                    if (!SoftwareHttpManager.IsOk(response))
                    {
                        this.StorePayload(softwareDataContent);
                    }

                    // call the kpm summary
                    try
                    {
                        Thread.Sleep(1000 * 5);
                        ProcessFetchDailyKpmTimerCallbackAsync(null);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        //
                    }
                    
                }
                else
                {
                    Logger.Info("Code Time metrics are currently paused.");
                    this.StorePayload(softwareDataContent);
                }

                _softwareData.ResetData();
                _lastPostTime = now;
            }
        }

        private void StorePayload(string softwareDataContent)
        {
            string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
            // append to the file
            File.AppendAllText(datastoreFile, softwareDataContent + Environment.NewLine);
        }

        private async void LaunchLoginPrompt()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            bool online = await SoftwareUserSession.IsOnlineAsync();

            if (online)
            {
                string msg = "To see your coding data in Code Time, please log in to your account.";
                const string caption = "Code Time";
                var result = MessageBox.Show(msg, caption,
                                             MessageBoxButtons.YesNo,
                                             MessageBoxIcon.Question);

                // If the no button was pressed ...
                if (result == DialogResult.Yes)
                {
                    // launch the browser
                    SoftwareCoUtil.launchLogin();
                }
            }
        }

        private async void SendOfflineData()
        {
            string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
            if (File.Exists(datastoreFile))
            {
                // get the content
                string[] lines = File.ReadAllLines(datastoreFile);

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
        }

        public static async void ProcessFetchDailyKpmTimerCallbackAsync(Object stateInfo)
        {
            if (!SoftwareCoUtil.isTelemetryOn())
            {
                Logger.Info("Code Time metrics are currently paused. Enable to update your metrics.");
                return;
            }
            bool online = await SoftwareUserSession.IsOnlineAsync();
            if (!online)
            {
                return;
            }
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/sessions?summary=true", null);
            if (SoftwareHttpManager.IsOk(response))
            {
                // get the json data
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);

                jsonObj.TryGetValue("currentDayMinutes", out object currentDayMinutes);
                long currentDayMinutesVal = (currentDayMinutes == null) ? 0 : Convert.ToInt64(currentDayMinutes);

                jsonObj.TryGetValue("averageDailyMinutes", out object averageDailyMinutes);
                long averageDailyMinutesVal = (averageDailyMinutes == null) ? 0 : Convert.ToInt64(averageDailyMinutes);

                string currentDayMinutesTime = SoftwareCoUtil.HumanizeMinutes(currentDayMinutesVal);
                string averageDailyMinutesTime = SoftwareCoUtil.HumanizeMinutes(averageDailyMinutesVal);

                // Code time today:  4 hrs | Avg: 3 hrs 28 min
                string inFlowIcon = currentDayMinutesVal > averageDailyMinutesVal ? "🚀" : "";
                string msg = string.Format("{0}{1}", inFlowIcon, currentDayMinutesTime);
                if (averageDailyMinutesVal > 0)
                {
                    msg += string.Format(" | {0}", averageDailyMinutesTime);
                }
                _softwareStatus.SetStatus(msg);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                FetchCodeTimeDashboardAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            } else
            {
                _softwareStatus.SetStatus("Code Time");
            }

        }

        private bool EnoughTimePassed(DateTime now)
        {
            return _lastPostTime < now.AddMinutes(postFrequency);
        }

        private string getSolutionDirectory()
        {
            if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
            {
                return Path.GetDirectoryName(ObjDte.Solution.FileName);
            }
            return null;
        }

        private void InitializeSoftwareData(string fileName)
        {
            if (_softwareData == null || !_softwareData.initialized)
            {

                // get the project name
                String projectName = "Untitled";
                String directoryName = "Unknown";
                if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
                {
                    projectName = Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName);
                    string solutionFile = ObjDte.Solution.FullName;
                    directoryName = Path.GetDirectoryName(solutionFile);
                }
                else
                {
                    directoryName = Path.GetDirectoryName(fileName);
                }

                if (_softwareData == null)
                {
                    ProjectInfo projectInfo = new ProjectInfo(projectName, directoryName);
                    _softwareData = new SoftwareData(projectInfo);
                }
                else
                {
                    _softwareData.project.name = projectName;
                    _softwareData.project.directory = directoryName;
                }
                _softwareData.initialized = true;
            }
            _softwareData.EnsureFileInfoDataIsPresent(fileName);
        }

        private async void InitializeUserInfo()
        {
            bool online = await SoftwareUserSession.IsOnlineAsync();
            bool softwareSessionFileExists = SoftwareCoUtil.softwareSessionFileExists();
            bool initializedUser = false;
            if (!softwareSessionFileExists)
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
                ProcessFetchDailyKpmTimerCallbackAsync(null);

                // send heartbeat
                SoftwareUserSession.SendHeartbeat("INITIALIZED");
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

        private static string NO_DATA = "CODE TIME\n\nNo data available\n";

        public static async Task FetchCodeTimeDashboardAsync()
        {
            string dashboardFile = SoftwareCoUtil.getDashboardFile();
            HttpResponseMessage resp =
                await SoftwareHttpManager.SendDashboardRequestAsync(HttpMethod.Get, "/dashboard");
            string content = NO_DATA;
            if (SoftwareHttpManager.IsOk(resp))
            {
                content = await resp.Content.ReadAsStringAsync();
            }

            if (File.Exists(dashboardFile))
            {
                File.SetAttributes(dashboardFile, FileAttributes.Normal);
            }
            File.WriteAllText(dashboardFile, content);
            File.SetAttributes(dashboardFile, FileAttributes.ReadOnly);
        }

        public static async void LaunchCodeTimeDashboardAsync()
        {
            await FetchCodeTimeDashboardAsync();
            string dashboardFile = SoftwareCoUtil.getDashboardFile();
            ObjDte.ItemOperations.OpenFile(dashboardFile);
        }

        protected async Task ShowOfflinePromptAsync() {
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
