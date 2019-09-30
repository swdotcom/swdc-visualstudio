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
        private int postFrequency = 1; // every minute

        private DateTime _lastPostTime = DateTime.UtcNow;
        private SoftwareData _softwareData;
        private static SessionSummary _sessionSummary;
        private SoftwareRepoManager _softwareRepoUtil;
        private static SoftwareStatus _softwareStatus;

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ONE_HOUR = ONE_MINUTE * 60;
        private static int THIRTY_MINUTES = ONE_MINUTE * 30;
        private static long lastDashboardFetchTime = 0;
        private static long day_in_sec = 60 * 60 * 24;
        private static int ZERO_SECOND = 1;
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
                _textDocKeyEvent.AfterKeyPress += AfterKeyPressedAsync;
                _docEvents.DocumentOpened += DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing += DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += DocEventsOnDocumentOpeningAsync;

                //initialize the StatusBar 
                await InitializeSoftwareStatusAsync();
                if (_sessionSummary == null)
                {
                    _sessionSummary = new SessionSummary();
                }
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

                

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                // setup timer to process events every 1 minute
                //timer = new System.Threading.Timer(
                //    ProcessSoftwareDataTimerCallbackAsync,
                //    autoEvent,
                //    ONE_MINUTE,
                //    ONE_MINUTE);

                offlineDataTimer = new System.Threading.Timer(
                      SendOfflineData,
                      null,
                      THIRTY_MINUTES,
                      THIRTY_MINUTES);

                // this.SendOfflineData();

                // start in 5 seconds every 5 min
                //int delay = 1000 * 5;
                //kpmTimer = new System.Threading.Timer(
                //    ProcessFetchDailyKpmTimerCallbackAsync,
                //    autoEvent,
                //    delay,
                //    ONE_MINUTE * 5);

                int delay = 1000 * 45;

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
                _textDocKeyEvent.AfterKeyPress -= AfterKeyPressedAsync;
                _docEvents.DocumentOpened -= DocEventsOnDocumentOpenedAsync;
                _docEvents.DocumentClosing -= DocEventsOnDocumentClosedAsync;
                _docEvents.DocumentSaved -= DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening -= DocEventsOnDocumentOpeningAsync;

                timer.Dispose();
                timer = null;

                // process any remaining data
               // ProcessSoftwareDataTimerCallbackAsync(null);
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

        private async void DocEventsOnDocumentOpeningAsync(String docPath, Boolean readOnly)
        {
            FileInfo fi = new FileInfo(docPath);
            String fileName = fi.FullName;
            InitializeSoftwareData(fileName);

            //Sets end and local_end for source file
            await _IntialisefileMap(fileName);
        }

        private async void AfterKeyPressedAsync(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
           String fileName = ObjDte.ActiveWindow.Document.FullName;
            InitializeSoftwareData(fileName);

            //Sets end and local_end for source file
            await _IntialisefileMap(fileName);

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

        private async void DocEventsOnDocumentOpenedAsync(Document document)
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
                //Sets end and local_end for source file
                await _IntialisefileMap(fileName);
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

        private async void DocEventsOnDocumentClosedAsync(Document document)
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
            //Sets end and local_end for source file
            await _IntialisefileMap(fileName);
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
            if (_softwareStatus != null)
            {
                _softwareStatus.ToggleStatusInfo();

            }
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

        public void HasData()
        {
           
            if (_softwareData.initialized && (_softwareData.keystrokes > 0 || _softwareData.source.Count > 0) && _softwareData.project != null && _softwareData.project.name != null)
            {
                SoftwareCoUtil.SetTimeout(ZERO_SECOND, PostData, false);
            }
            
        }

        public void PostData()
        {
            Logger.Info(DateTime.Now.ToString() + "PostData");
            double offset = 0;
            long end = 0;
            long local_end = 0;

            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            DateTime now    = DateTime.UtcNow;
            
                offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
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

                foreach (KeyValuePair<string, object> sourceFiles in _softwareData.source)
                {

                    JsonObject fileInfoData = null;
                    fileInfoData = (JsonObject)sourceFiles.Value;
                    object outend;
                    fileInfoData.TryGetValue("end", out outend);

                    if (long.Parse(outend.ToString()) == 0)
                    {
                        end = nowTime.now;
                        local_end = nowTime.local_now;
                        _softwareData.addOrUpdateFileInfo(sourceFiles.Key, "end", end);
                        _softwareData.addOrUpdateFileInfo(sourceFiles.Key, "local_end", local_end);

                    }

                try
                {

                    _softwareData.end = nowTime.now;
                    _softwareData.local_end = nowTime.local_now;

                }
                catch (Exception)

                {

                }
                string softwareDataContent = _softwareData.GetAsJson();
                Logger.Info("Code Time: sending: " + softwareDataContent);

                if (SoftwareCoUtil.isTelemetryOn())
                {
                    StorePayload(_softwareData);

                }
                else
                {
                    Logger.Info("Code Time metrics are currently paused.");

                }

                _softwareData.ResetData();
                _lastPostTime = now;
            }
        }



        // This method is called by the timer delegate.
        //private async void ProcessSoftwareDataTimerCallbackAsync(Object stateInfo)
        //{
        //    AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;
        //    double offset   = 0;
        //    long end        = 0;
        //    long local_end  = 0;

        //    NowTime nowTime = SoftwareCoUtil.GetNowTime();
        //    DateTime now    = DateTime.UtcNow;
        //    if (_softwareData != null && _softwareData.HasData() && (EnoughTimePassed(now) || timer == null))
        //    {
        //         offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
        //        //_softwareData.local_start = _softwareData.start + ((int)offset * 60);
        //        _softwareData.offset = Math.Abs((int)offset);
        //        if (TimeZone.CurrentTimeZone.DaylightName != null
        //            && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
        //        {
        //            _softwareData.timezone = TimeZone.CurrentTimeZone.DaylightName;
        //        }
        //        else
        //        {
        //            _softwareData.timezone = TimeZone.CurrentTimeZone.StandardName;
        //        }

        //        foreach (KeyValuePair<string, object> sourceFiles in _softwareData.source)
        //        {

        //            JsonObject fileInfoData = null;
        //            fileInfoData = (JsonObject)sourceFiles.Value;
        //            object outend;
        //            fileInfoData.TryGetValue("end", out outend);

        //            if (long.Parse(outend.ToString()) == 0)
        //            {
        //                //end         = SoftwareCoUtil.getNowInSeconds();
        //                //offset      = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
        //                //local_end   = end + ((int)offset * 60);
        //                end           = nowTime.now;
        //                local_end     = nowTime.local_now;
        //                _softwareData.addOrUpdateFileInfo(sourceFiles.Key, "end", end);
        //                _softwareData.addOrUpdateFileInfo(sourceFiles.Key, "local_end", local_end);

        //            }

        //        }

        //        try
        //        {
        //            //end         = SoftwareCoUtil.getNowInSeconds();
        //            //offset      = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
        //            //local_end   = end + ((int)offset * 60);

        //            _softwareData.end       = nowTime.now;
        //            _softwareData.local_end = nowTime.local_now;

        //        }
        //        catch (Exception)

        //        {

        //        }
        //        string softwareDataContent = _softwareData.GetAsJson();
        //        Logger.Info("Code Time: sending: " + softwareDataContent);

        //        if (SoftwareCoUtil.isTelemetryOn())
        //        {

        //             StorePayload(_softwareData);

        //            // call the kpm summary
        //           /* try
        //            {
        //                Thread.Sleep(1000 * 5);
        //                ProcessFetchDailyKpmTimerCallbackAsync(null);
        //            }
        //            catch (ThreadInterruptedException e)
        //            {
        //                //
        //            }*/

        //        }
        //        else
        //        {
        //            Logger.Info("Code Time metrics are currently paused.");
        //           // this.StorePayload(softwareDataContent);
        //        }

        //        _softwareData.ResetData();
        //        _lastPostTime = now;
        //    }
        //}

        private void StorePayload(SoftwareData _softwareData)
        {
            if (_softwareData != null)
            {

                long keystrokes = _softwareData.keystrokes;

                incrementSessionSummaryData(1 /*minutes*/, keystrokes);

                saveSessionSummaryToDisk(_sessionSummary);

                string softwareDataContent = _softwareData.GetAsJson();

                string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
                // append to the file
                File.AppendAllText(datastoreFile, softwareDataContent + Environment.NewLine);

                //// update the statusbar
                fetchSessionSummaryInfoAsync();
            }
        }

        private void incrementSessionSummaryData(int minute, long keystrokes)
        {
            _sessionSummary = getSessionSummayData();
            _sessionSummary.currentDayMinutes += minute;
            _sessionSummary.currentDayKeystrokes += keystrokes;
        }


        private static SessionSummary getSessionSummayData()
        {
            if (SoftwareCoUtil.SessionSummaryFileExists())
            {
                string sessionSummary = SoftwareCoUtil.getSessionSummaryFileData();
                if (!string.IsNullOrEmpty(sessionSummary))
                {
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(sessionSummary);
                    _sessionSummary = DictionaryToObject<SessionSummary>(jsonObj);
                }
                else
                    return _sessionSummary;
            }
            return _sessionSummary;
        }

        /*private void StorePayload(string softwareDataContent)
        {
            string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
            // append to the file
            File.AppendAllText(datastoreFile, softwareDataContent + Environment.NewLine);
        }*/

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

        private async void SendOfflineData(object stateinfo)
        {
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

            ÇlearSessionSummaryData();

            fetchSessionSummaryInfoAsync(true);

        }

        private void ÇlearSessionSummaryData()
        {
            if (_sessionSummary != null)
            {
                _sessionSummary.averageDailyKeystrokes = 0;
                _sessionSummary.averageDailyMinutes = 0;
                _sessionSummary.currentDayKeystrokes = 0;
                _sessionSummary.currentDayMinutes = 0;
                _sessionSummary.liveshareMinutes = 0;
            }
            saveSessionSummaryToDisk(_sessionSummary);
        }

        public static async Task fetchSessionSummaryInfoAsync(bool forceRefresh = false)
        {
            //SessionSummary sessionSummary = new SessionSummary();

            var sessionSummaryResult = await GetSessionSummaryStatusAsync(forceRefresh);

            if (sessionSummaryResult.status == "OK")
            {
                await FetchCodeTimeDashboardAsync(sessionSummaryResult.sessionSummary);
            }


        }
        private static async Task<SessionSummaryResult> GetSessionSummaryStatusAsync(bool forceRefresh = false)
        {
            SessionSummaryResult sessionSummaryResult = new SessionSummaryResult();
            _sessionSummary = getSessionSummayData();

            //if (SoftwareCoUtil.SessionSummaryFileExists())
            //{

                if (_sessionSummary.currentDayMinutes == 0 || forceRefresh)
                {
                    bool online = await SoftwareUserSession.IsOnlineAsync();

                    if (!online)
                    {
                        sessionSummaryResult.sessionSummary = _sessionSummary;
                        sessionSummaryResult.status = "OK";
                        updateStatusBarWithSummaryData();
                        return sessionSummaryResult;
                    }
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/sessions/summary", null);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                        _sessionSummary = DictionaryToObject<SessionSummary>(jsonObj);

                        saveSessionSummaryToDisk(_sessionSummary);

                        updateStatusBarWithSummaryData();

                        sessionSummaryResult.sessionSummary = _sessionSummary;
                        sessionSummaryResult.status = "OK";
                    }

                }
                else
                {
                    updateStatusBarWithSummaryData();
                }

            //}
            //else
            //{
            //    updateStatusBarWithSummaryData();
            //}

            sessionSummaryResult.sessionSummary = _sessionSummary;
            sessionSummaryResult.status = "OK";
            return sessionSummaryResult;
        }
        private static T DictionaryToObject<T>(IDictionary<string, object> dict) where T : new()
        {
            var t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                KeyValuePair<string, object> item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));

                // Find which property type (int, string, double? etc) the CURRENT property is...
                Type tPropertyType = t.GetType().GetProperty(property.Name).PropertyType;

                // Fix nullables...
                Type newT = Nullable.GetUnderlyingType(tPropertyType) ?? tPropertyType;

                // ...and change the type
                object newA = Convert.ChangeType(item.Value, newT);
                t.GetType().GetProperty(property.Name).SetValue(t, newA, null);
            }
            return t;
        }

        private static void updateStatusBarWithSummaryData()
        {
            _sessionSummary = getSessionSummayData();

            long currentDayMinutesVal = _sessionSummary.currentDayMinutes;
            long averageDailyMinutesVal = _sessionSummary.averageDailyMinutes;

            string currentDayMinutesTime = SoftwareCoUtil.HumanizeMinutes(currentDayMinutesVal);
            string averageDailyMinutesTime = SoftwareCoUtil.HumanizeMinutes(averageDailyMinutesVal);

            // Code time today:  4 hrs | Avg: 3 hrs 28 min
            string inFlowIcon = currentDayMinutesVal > averageDailyMinutesVal ? "🚀" : "";
            string msg = string.Format("{0}{1}", inFlowIcon, currentDayMinutesTime);

            if (averageDailyMinutesVal > 0)
            {
                msg += string.Format(" | {0}", averageDailyMinutesTime);
                _softwareStatus.SetStatus(msg);
            }
            else if(currentDayMinutesVal>0)
            {
                _softwareStatus.SetStatus(msg);
            }
            else
            {
                _softwareStatus.SetStatus("Code Time");
            }

        }

        private static void saveSessionSummaryToDisk(SessionSummary sessionSummary)
        {
            string sessionSummaryFile = SoftwareCoUtil.getSessionSummaryFile();


            if (SoftwareCoUtil.SessionSummaryFileExists())
            {
                File.SetAttributes(sessionSummaryFile, FileAttributes.Normal);
            }

            try
            {
                //SoftwareCoUtil.WriteToFileThreadSafe(sessionSummary.GetSessionSummaryAsJson(), sessionSummaryFile);
                File.WriteAllText(sessionSummaryFile, sessionSummary.GetSessionSummaryAsJson());
                File.SetAttributes(sessionSummaryFile, FileAttributes.ReadOnly);
            }
            catch (Exception e)
            {


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
                NowTime nowTime = SoftwareCoUtil.GetNowTime(); 
                     
                // get the project name
                String projectName      = "Untitled";
                String directoryName    = "Unknown";
                if (ObjDte.Solution != null && ObjDte.Solution.FullName != null && !ObjDte.Solution.FullName.Equals(""))
                {
                    projectName         = Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName);
                    string solutionFile = ObjDte.Solution.FullName;
                    directoryName       = Path.GetDirectoryName(solutionFile);
                }
                else
                {
                    directoryName = Path.GetDirectoryName(fileName);
                }

                if (_softwareData == null)
                {
                    ProjectInfo projectInfo = new ProjectInfo(projectName, directoryName);
                    _softwareData           = new SoftwareData(projectInfo);
                  
                }
                else
                {
                    _softwareData.project.name = projectName;
                    _softwareData.project.directory = directoryName;
                }
                _softwareData.start         = nowTime.now;
                _softwareData.local_start   = nowTime.local_now;
                _softwareData.initialized   = true;
                SoftwareCoUtil.SetTimeout(ONE_MINUTE, HasData, false);
            }
            _softwareData.EnsureFileInfoDataIsPresent(fileName);
        }
        private async Task _IntialisefileMap(string fileName)
        {

            foreach (KeyValuePair<string, object> sourceFiles in _softwareData.source)
            {
                long end        = 0;
                long local_end  = 0;
               // double offset   = 0;
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                if (fileName != sourceFiles.Key)
                {
                    object outend           = null;
                    JsonObject fileInfoData = null;
                    fileInfoData            = (JsonObject)sourceFiles.Value;
                    fileInfoData.TryGetValue("end", out outend);

                    if (long.Parse(outend.ToString()) == 0)
                    {

                        //end         = SoftwareCoUtil.getNowInSeconds();
                        //offset      = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
                        //local_end   = end + ((int)offset * 60);
                        end         = nowTime.now;
                        local_end   = nowTime.local_now;

                        _softwareData.addOrUpdateFileInfo(fileName, "end", end);
                        _softwareData.addOrUpdateFileInfo(fileName, "local_end", local_end);

                    }

                }
                else
                {
                    _softwareData.addOrUpdateFileInfo(fileName, "end", 0);
                    _softwareData.addOrUpdateFileInfo(fileName, "local_end", 0);
                }

            }

        }
        private async void InitializeUserInfo()
        {
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
                fetchSessionSummaryInfoAsync();

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

        private static async Task FetchCodeTimeDashboardAsync(SessionSummary _sessionSummary)
        {
            string summaryContent = "";
            string summaryInfoFile = SoftwareCoUtil.getSessionSummaryInfoFile();

            bool online = await SoftwareUserSession.IsOnlineAsync();
            
            long diff = SoftwareCoUtil.getNowInSeconds() - lastDashboardFetchTime;
            if (lastDashboardFetchTime == 0 || diff >= day_in_sec || !online)
            {
  
                if (!online)
                {
                    lastDashboardFetchTime = 0;
                }
                else
                lastDashboardFetchTime = SoftwareCoUtil.getNowInSeconds();

                HttpResponseMessage resp =
                await SoftwareHttpManager.SendDashboardRequestAsync(HttpMethod.Get, "/dashboard?showMusic=false&showGit=false&showRank=false&showToday=false");

                if (SoftwareHttpManager.IsOk(resp))
                {
                    summaryContent += await resp.Content.ReadAsStringAsync();
                }
                else
                {
                    summaryContent = NO_DATA;
                }


                if (File.Exists(summaryInfoFile))
                {
                    File.SetAttributes(summaryInfoFile, FileAttributes.Normal);
                }

                try
                {

                    File.WriteAllText(summaryInfoFile, summaryContent);
                    File.SetAttributes(summaryInfoFile, FileAttributes.ReadOnly);
                }
                catch (Exception e)
                {


                }

            }
            string dashboardFile = SoftwareCoUtil.getDashboardFile();
            string dashboardContent = "";
            string suffix = SoftwareCoUtil.CreateDateSuffix(DateTime.Now);
            string formattedDate = DateTime.Now.ToString("ddd, MMM ") + suffix + DateTime.Now.ToString(" h:mm tt");

            dashboardContent = "CODE TIME          " + "(Last updated on " + formattedDate + " )";
            dashboardContent += "\n\n";

            string todayDate = DateTime.Now.ToString("ddd, MMM ") + suffix;
            string today_date = "Today " + "(" + todayDate + ")";
            dashboardContent += SoftwareCoUtil.getSectionHeader(today_date);

            if (_sessionSummary != null)
            {

                string averageTime = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.averageDailyMinutes);
                string hoursCodedToday = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.currentDayMinutes);
                String liveshareTime = "";
                //if (_sessionSummary.liveshareMinutes != 0)
                //{
                //    liveshareTime = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.liveshareMinutes);
                //}
                dashboardContent += SoftwareCoUtil.getDashboardRow("Hours Coded", hoursCodedToday);
                dashboardContent += SoftwareCoUtil.getDashboardRow("90-day avg", averageTime);
                //if (liveshareTime != "0")
                //{
                //    dashboardContent += SoftwareCoUtil.getDashboardRow("Live Share", liveshareTime);
                //}
                dashboardContent += "\n";
            }

            if (SoftwareCoUtil.SessionSummaryInfoFileExists())
            {
                string SummaryData = SoftwareCoUtil.getSessionSummaryInfoFileData();
                dashboardContent += SummaryData;
            }


            if (File.Exists(dashboardFile))
            {
                File.SetAttributes(dashboardFile, FileAttributes.Normal);
            }
            try
            {
                //SoftwareCoUtil.WriteToFileThreadSafe(dashboardContent, dashboardFile);
                File.WriteAllText(dashboardFile, dashboardContent);
                File.SetAttributes(dashboardFile, FileAttributes.ReadOnly);

            }
            catch (Exception e)
            {

            }


        }

        public static async void LaunchCodeTimeDashboardAsync()
        {
            fetchSessionSummaryInfoAsync();
            string dashboardFile = SoftwareCoUtil.getDashboardFile();
            if(File.Exists(dashboardFile))
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
        internal class SessionSummaryResult
        {
            public SessionSummary sessionSummary { get; set; }
            public string status { get; set; }
        }
    }
}
