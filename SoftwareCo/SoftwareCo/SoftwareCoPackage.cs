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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(SoftwareCoPackage.PackageGuidString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class SoftwareCoPackage : Package
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

        private Timer timer;
        private Timer docChangeTimer;
        private Timer kpmTimer;
        private Timer repoMemberTimer;
        private Timer repoCommitsTimer;

        // Used by Constants for version info
        public static DTE2 ObjDte;

        // this is the solution full name
        private string _solutionName = string.Empty;
        private string _lastDocument;
        private int postFrequency = 1; // every minute

        private DateTime _lastPostTime = DateTime.UtcNow;
        private SoftwareData _softwareData;
        private SoftwareCoUtil _softwareUtil;
        private SoftwareRepoUtil _softwareRepoUtil;

        private bool _isOnline = true;
        private bool _isAuthenticated = true;
        private bool _hasJwt = true;
        private bool _hasToken = false;

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
        protected override void Initialize()
        {
            base.Initialize();

            ObjDte = (DTE2)GetService(typeof(DTE));
            _dteEvents = ObjDte.Events.DTEEvents;
            _dteEvents.OnStartupComplete += OnOnStartupComplete;

            Task.Run(() =>
            {
                InitializeAsync();
            });
        }

        public void InitializeAsync()
        {
            try
            {
                Logger.Info(string.Format("Initializing SoftwareCo v{0}", Constants.PluginVersion));

                // VisualStudio Object
                Events2 events = (Events2)ObjDte.Events;
                _textDocKeyEvent = events.TextDocumentKeyPressEvents;
                _docEvents = ObjDte.Events.DocumentEvents;
                _windowEvents = ObjDte.Events.WindowEvents;

                // setup event handlers
                _textDocKeyEvent.AfterKeyPress += AfterKeyPressed;
                _docEvents.DocumentOpened += DocEventsOnDocumentOpened;
                _docEvents.DocumentClosing += DocEventsOnDocumentClosed;
                _docEvents.DocumentSaved += DocEventsOnDocumentSaved;
                _docEvents.DocumentOpening += DocEventsOnDocumentOpening;
                _windowEvents.WindowActivated += WindowEventsOnWindowActivated;

                // initialize the menu commands
                SoftwareLaunchCommand.Initialize(this);
                SoftwareEnableMetricsCommand.Initialize(this);
                SoftwarePauseMetricsCommand.Initialize(this);

                if (_softwareUtil == null)
                {
                    _softwareUtil = new SoftwareCoUtil();
                }

                if (_softwareRepoUtil == null)
                {
                    _softwareRepoUtil = new SoftwareRepoUtil();
                }

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                // setup timer to process events every 1 minute
                timer = new Timer(
                    ProcessSoftwareDataTimerCallbackAsync,
                    autoEvent,
                    ONE_MINUTE,
                    ONE_MINUTE);

                // every 30 seconds
                docChangeTimer = new Timer(
                    CheckFileLengthUpdates,
                    autoEvent,
                    ONE_MINUTE,
                    1000 * 30);

                this.SendOfflineData();

                // start in 5 seconds every 1 min
                int delay = 1000 * 5;
                kpmTimer = new Timer(
                    ProcessFetchDailyKpmTimerCallbackAsync,
                    autoEvent,
                    delay,
                    ONE_MINUTE);

                delay = 1000 * 45;
                repoMemberTimer = new Timer(
                    ProcessRepoMembers,
                    autoEvent,
                    delay,
                    ONE_HOUR);

                delay = ONE_MINUTE + (1000 * 10);
                repoCommitsTimer = new Timer(
                    ProcessRepoCommits,
                    autoEvent,
                    delay,
                    ONE_HOUR);

                this.AuthenticationNotificationCheck();
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
                _windowEvents.WindowActivated -= WindowEventsOnWindowActivated;

                timer.Dispose();
                timer = null;

                // process any remaining data
                ProcessSoftwareDataTimerCallbackAsync(null);
            }
        }
        #endregion

        #region Event Handlers

        private int getLineCount(string fileName)
        {
            int counter = 0;
            using (var file = new StreamReader(fileName))
            {
                while (file.ReadLine() != null)
                {
                    counter++;
                }
            }
            return counter;
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            InitializeSoftwareData();
            String fileName = document.FullName;
            FileInfo fi = new FileInfo(fileName);
            /**
            long prevLen = _softwareData.getFileInfoDataForProperty(fi.FullName, "length");
            long diff = (fi != null && prevLen > 0) ? fi.Length - prevLen : 0;
            if (diff > 1)
            {
                // register a copy and past event
                _softwareData.UpdateData(fileName, "paste", 1);
                Logger.Info("Software.com: Copy+Paste incremented");
            }**/
            _softwareData.UpdateData(fileName, "length", fi.Length);
        }

        private void DocEventsOnDocumentOpening(String docPath, Boolean readOnly)
        {
            InitializeSoftwareData();
            FileInfo fi = new FileInfo(docPath);
            // update the length of this 
            if (_softwareData != null && fi != null)
            {
                _softwareData.UpdateData(fi.FullName, "length", fi.Length);
            }
        }

        private void AfterKeyPressed(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
            InitializeSoftwareData();

            if (_softwareData.project.identifier == null || _softwareData.project.identifier.Equals(""))
            {
                IDictionary<string, string> resourceInfo = _softwareRepoUtil.GetResourceInfo(_softwareData.project.directory);
                if (resourceInfo.ContainsKey("identifier"))
                {
                    resourceInfo.TryGetValue("identifier", out string itentifierObj);
                    String identifier = (itentifierObj == null) ? null : Convert.ToString(itentifierObj);
                    if (identifier != null)
                    {
                        _softwareData.project.identifier = identifier;
                        _softwareData.project.resource = resourceInfo;
                    }
                    
                } else
                {
                    // fill it with the directory so we don't keep trying and causing latency
                    _softwareData.project.identifier = _softwareData.project.directory;
                }
            }

            String fileName = ObjDte.ActiveWindow.Document.FullName;

            if (ObjDte.ActiveWindow.Document.Language != null)
            {
                _softwareData.addOrUpdateFileStringInfo(fileName, "syntax", ObjDte.ActiveWindow.Document.Language);
            }
            if (!String.IsNullOrEmpty(Keypress))
            {

                long prevLen = _softwareData.getFileInfoDataForProperty(fileName, "length");
                FileInfo fi = new FileInfo(fileName);
                if (fi != null)
                {
                    // update the file length
                    if (fi.Length != prevLen)
                    {
                        _softwareData.addOrUpdateFileInfo(fileName, "length", fi.Length);
                    } else
                    {
                        long newLen = prevLen + 1;
                        _softwareData.addOrUpdateFileInfo(fileName, "length", newLen);
                    }
                }

                bool isNewLine = false;
                if (Keypress == "\b")
                {
                    // register a delete event
                    _softwareData.UpdateData(fileName, "delete", 1);
                    Logger.Info("Software.com: Delete character incremented");
                }
                else if (Keypress == "\r")
                {
                    isNewLine = true;
                }
                else
                {
                    _softwareData.UpdateData(fileName, "add", 1);
                    Logger.Info("Software.com: KPM incremented");
                }
                

                if (isNewLine)
                {
                    _softwareData.addOrUpdateFileInfo(fileName, "linesAdded", 1);
                }

                long lineCount = _softwareData.getFileInfoDataForProperty(fileName, "lines");
                if (lineCount == 0 || isNewLine)
                {
                    lineCount = this.getLineCount(fileName);
                    _softwareData.addOrUpdateFileInfo(fileName, "lines", lineCount);
                }

                _softwareData.keystrokes += 1;

            }
        }

        private void DocEventsOnDocumentOpened(Document document)
        {
            try
            {
                HandleDocumentEventActivity(document.FullName, false);
                if (_softwareData != null)
                {
                    _softwareData.UpdateData(document.FullName, "open", 1);
                    Logger.Info("Software.com: File open incremented");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentOpened", ex);
            }
        }

        private void DocEventsOnDocumentClosed(Document document)
        {
            try
            {
                HandleDocumentEventActivity(document.FullName, true);
                if (_softwareData != null)
                {
                    _softwareData.UpdateData(document.FullName, "close", 1);
                    Logger.Info("Software.com: File close incremented");
                }
            }
            catch (Exception ex)
            {
                Logger.Error("DocEventsOnDocumentClosed", ex);
            }
        }

        private void WindowEventsOnWindowActivated(Window gotFocus, Window lostFocus)
        {
            try
            {
                var document = ObjDte.ActiveWindow.Document;
                if (document != null)
                {
                    HandleDocumentEventActivity(document.FullName, false);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("WindowEventsOnWindowActivated", ex);
            }
        }

        private void OnOnStartupComplete()
        {
            //
        }
        #endregion

        #region Methods
        private void HandleDocumentEventActivity(string currentFile, bool isWrite)
        {
            if (currentFile == null)
            {
                return;
            }

            if (currentFile.Equals(_lastDocument))
            {
                return;
            }

            _lastDocument = currentFile;

            InitializeSoftwareData();
        }

        private void CheckFileLengthUpdates(Object stateInfo)
        {
            if (ObjDte.ActiveDocument != null && ObjDte.ActiveWindow.Document != null)
            {
                String fileName = ObjDte.ActiveWindow.Document.FullName;
                if (fileName != null && !fileName.Equals(""))
                {
                    InitializeSoftwareData();

                    // update the length for this file so we can correctly compute the paste diff
                    FileInfo fi = new FileInfo(fileName);
                    if (fi != null)
                    {
                        _softwareData.UpdateData(fileName, "length", fi.Length);
                    }
                }
            }
            
        }

        private void ProcessRepoMembers(Object stateInfo)
        {
            InitializeSoftwareData();

            if (_softwareData != null && _softwareData.project.directory != null)
            {

                _softwareRepoUtil.GetRepoUsers(_softwareData.project.directory);
            }
        }

        private void ProcessRepoCommits(Object stateInfo)
        {
            InitializeSoftwareData();

            if (_softwareData != null && _softwareData.project.directory != null)
            {

                _softwareRepoUtil.GetHistoricalCommitsAsync(_softwareData.project.directory);
            }
        }

        // This method is called by the timer delegate.
        private async void ProcessSoftwareDataTimerCallbackAsync(Object stateInfo)
        {
            AutoResetEvent autoEvent = (AutoResetEvent)stateInfo;

            this.CheckAuthStatus();
            this.SendOfflineData();

            DateTime now = DateTime.UtcNow;
            if (_softwareData != null && _softwareData.HasData() && (EnoughTimePassed(now) || timer == null))
            {
                //
                // Ensure the doc name is added in case we're in the middle of capturing
                // keystrokes and the user hasn't save the doc to trigger this yet
                //
                var document = ObjDte.ActiveWindow.Document;
                if (document != null)
                {
                    HandleDocumentEventActivity(document.FullName, false);
                }
                double offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
                _softwareData.local_start = _softwareData.start + ((int)offset * 60);
                _softwareData.offset = Math.Abs((int)offset);
                if (TimeZone.CurrentTimeZone.DaylightName != null
                    && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
                {
                    _softwareData.timezone = TimeZone.CurrentTimeZone.DaylightName;
                } else {
                    _softwareData.timezone = TimeZone.CurrentTimeZone.StandardName;
                }

                string softwareDataContent = _softwareData.GetAsJson();
                Logger.Info("Software.com: sending: " + softwareDataContent);

                /**
                 * the payload needs to look like this
                 {
                  "start":1529683309,"end":1529683369,"data":9,"pluginId":6,
                  "source":{
  	                "C:\\Users\\Xavier Luiz\\source\\repos\\UnitTestProject3\\UnitTestProject3\\UnitTest1.cs":{
    	                "paste":0,"open":0,"close":0,"delete":0,"keys":9,"add":9,
                                "netkeys":9,"length":569,"lines":0,"linesAdded":0,"linesRemoved":0,"syntax":""
  	                }
                  },
                  "type":"Events",
                  "project":{"name":"UnitTestProject3","directory":"C:\\Users\\Xavier Luiz\\source\\repos"}
                }
                 **/
                
                if (_softwareUtil.isTelemetryOn())
                {

                    HttpResponseMessage response = await _softwareUtil.SendRequestAsync(HttpMethod.Post, "/data", softwareDataContent);

                    if (!_softwareUtil.IsOk(response))
                    {
                        this.StorePayload(softwareDataContent);
                        this.AuthenticationNotificationCheck();
                    }
                }
                else
                {
                    Logger.Info("Software.com metrics are currently paused.");
                    this.StorePayload(softwareDataContent);
                }

                _softwareData.ResetData();
                _lastPostTime = now;
                _lastDocument = null;
            }
        }

        private void StorePayload(string softwareDataContent)
        {
            string datastoreFile = _softwareUtil.getSoftwareDataStoreFile();
            // append to the file
            File.AppendAllText(datastoreFile, softwareDataContent + Environment.NewLine);
        }

        private void CheckAuthStatus()
        {
            this.HasJwt();
            this.IsOnline();
            this.IsAuthenticated();
        }

        private async void IsOnline()
        {
            HttpResponseMessage response = await _softwareUtil.SendRequestAsync(HttpMethod.Get, "/ping", null);
            this._isOnline = _softwareUtil.IsOk(response);
            this.UpdateStatus();
        }

        private async void IsAuthenticated()
        {
            HttpResponseMessage response = await _softwareUtil.SendRequestAsync(HttpMethod.Get, "/users/ping", null);
            this._isAuthenticated = _softwareUtil.IsOk(response);
            this.UpdateStatus();
        }

        private bool HasJwt()
        {
            object jwt = _softwareUtil.getItem("jwt");
            this._hasJwt = (jwt != null && !((string)jwt).Equals(""));
            this.UpdateStatus();
            return this._hasJwt;
        }

        private bool HasToken()
        {
            object token = _softwareUtil.getItem("token");
            this._hasToken = (token != null && !((string)token).Equals(""));
            return this._hasToken;
        }

        private void UpdateStatus()
        {
            if (!this._hasJwt || !this._isAuthenticated || !this._isOnline)
            {
                this.SetStatus("Software.com");
            }
        }

        private void AuthenticationNotificationCheck()
        {
            object lastUpdateTimeObj = _softwareUtil.getItem("vs_lastUpdateTime");
            long lastUpdate = (lastUpdateTimeObj != null) ? (long)lastUpdateTimeObj : 0;
            long nowInSec = _softwareUtil.getNowInSeconds();

            if ((this.HasJwt() && this._isAuthenticated) || !this._isOnline)
            {
                // we're already authenticated or we're not online to begin with
                return;
            }

            if (this.HasToken())
            {
                // we have a token so only update every 4 hours
                if ((nowInSec - lastUpdate) < (60 * 60 * 4))
                {
                    // not over 4 hours yet
                    return;
                }
            } else
            {
                // no token, update after 5 minutes
                if ((nowInSec - lastUpdate) < (60 * 5))
                {
                    // not over 5 minutes yet
                    return;
                }
            }

            _softwareUtil.setItem("vs_lastUpdateTime", nowInSec);

            string msg = "To see your coding data in Software.com, please log in to your account.";

            Guid clsid = Guid.Empty;
            int result;
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            uiShell.ShowMessageBox(
                0,
                ref clsid,
                string.Empty,
                msg,
                string.Empty,
                0,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                OLEMSGICON.OLEMSGICON_INFO,
                0,
                out result);
            // ok_cancel = 1
            Logger.Info("Selected result: " + result);
            if (result == 1)
            {
                // launch the browser
                _softwareUtil.launchSoftwareDashboard();
            }
        }

        private async void SendOfflineData()
        {
            string datastoreFile = _softwareUtil.getSoftwareDataStoreFile();
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
                    HttpResponseMessage response = await _softwareUtil.SendRequestAsync(HttpMethod.Post, "/data/batch", jsonContent);
                    if (_softwareUtil.IsOk(response))
                    {
                        // delete the file
                        File.Delete(datastoreFile);
                    }
                }
            }
        }

        private async void ProcessFetchDailyKpmTimerCallbackAsync(Object stateInfo)
        {
            if (!_softwareUtil.isTelemetryOn())
            {
                Logger.Info("Software.com metrics are currently paused. Enable to update your metrics.");
                return;
            }
            long nowInSec = _softwareUtil.getNowInSeconds();
            HttpResponseMessage response = await _softwareUtil.SendRequestAsync(HttpMethod.Get, "/sessions?summary=true&from=" + nowInSec, null);
            if (_softwareUtil.IsOk(response))
            {
                // get the json data
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);

                jsonObj.TryGetValue("inFlow", out object inFlowObj);
                bool inFlow = (inFlowObj == null) ? true : Convert.ToBoolean(inFlowObj);
                    
                jsonObj.TryGetValue("lastKpm", out object lastKpm);
                long lastKpmVal = (lastKpm == null) ? 0 : Convert.ToInt64(lastKpm);

                jsonObj.TryGetValue("currentSessionGoalPercent", out object currentSessionGoalPercent);
                double currentSessionGoalPercentVal = (currentSessionGoalPercent == null) ? 0.0 : Convert.ToDouble(currentSessionGoalPercent);
                    
                jsonObj.TryGetValue("currentSessionMinutes", out object currentSessionMinutes);
                long currentSessionMinutesVal = (currentSessionMinutes == null) ? 0 : Convert.ToInt64(currentSessionMinutes);

                string sessionTimeIcon = "";
                if (currentSessionGoalPercentVal > 0)
                {
                    if (currentSessionGoalPercentVal < 0.4)
                    {
                        sessionTimeIcon = "🌘";
                    }
                    else if (currentSessionGoalPercentVal < 0.7)
                    {
                        sessionTimeIcon = "🌗";
                    }
                    else if (currentSessionGoalPercentVal < 0.93)
                    {
                        sessionTimeIcon = "🌖";
                    }
                    else if (currentSessionGoalPercentVal < 1.3)
                    {
                        sessionTimeIcon = "🌕";
                    }
                    else
                    {
                        sessionTimeIcon = "🌔";

                    }
                }

                string sessionTime = "";
                if (currentSessionMinutesVal == 60)
                {
                    sessionTime = "1 hr";
                }
                else if (currentSessionMinutesVal > 60)
                {
                    string formatedHrs = String.Format("{0:0.00}", (currentSessionMinutesVal / 60));
                    sessionTime = formatedHrs + " hrs";
                }
                else if (currentSessionMinutesVal == 1)
                {
                    sessionTime = "1 min";
                }
                else
                {
                    sessionTime = currentSessionMinutesVal + " min";
                }
                    
                if (lastKpmVal > 0 || currentSessionMinutesVal > 0)
                {
                    string kpmMsg = lastKpmVal + " KPM";
                    if (inFlow) {
                        kpmMsg = "🚀" + " " + kpmMsg;
                    }
                    string sessionMsg = sessionTime;
                    if (!sessionTimeIcon.Equals(""))
                    {
                        sessionMsg = sessionTimeIcon + " " + sessionTime;
                    }
                    this.SetStatus("<S> " + kpmMsg + ", " + sessionMsg);
                }
                else
                {
                    this.SetStatus("Software.com");
                }
            }
            
        }

        private bool EnoughTimePassed(DateTime now)
        {
            return _lastPostTime < now.AddMinutes(postFrequency);
        }

        private void InitializeSoftwareData()
        {

            if (_softwareData == null || String.IsNullOrEmpty(_softwareData.project.directory))
            {
                String projectName = (ObjDte.Solution != null) ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName) : "None";
                String solutionFileName = (ObjDte.Solution != null) ? ObjDte.Solution.FileName : "";

                String directoryName = Path.GetDirectoryName(solutionFileName);
                if (_softwareData == null)
                {
                    ProjectInfo projectInfo = new ProjectInfo(projectName, directoryName);
                    _softwareData = new SoftwareData(projectInfo);
                } else
                {
                    _softwareData.project.name = projectName;
                    _softwareData.project.directory = directoryName;
                }
            }
         
        }

        private String getDownloadDestinationDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void SetStatus(string msg)
        {
            IVsStatusbar statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusbar.SetText(msg);
        }

        #endregion
        }
}
