using System;
using System.Diagnostics;
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
using System.ComponentModel;
using System.Collections.Generic;

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
    public sealed class SoftwareCoPackage : Package
    {
        #region fields
        /// <summary>
        /// SoftwareCoPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "0ae38c4e-1ac5-4457-bdca-bb2dfc342a1c";

        private const string PLUGIN_MGR_ENDPOINT = "http://localhost:19234/api/v1/data";
        private DTEEvents _dteEvents;
        private DocumentEvents _docEvents;
        private WindowEvents _windowEvents;
        private TextDocumentKeyPressEvents _textDocKeyEvent;

        private Timer timer;
        private Timer kpmTimer;

        // Used by Constants for version info
        public static DTE2 ObjDte;

        // this is the solution full name
        private string _solutionName = string.Empty;
        private string _lastDocument;
        private int postFrequency = 1; // every minute

        private DateTime _lastPostTime = DateTime.UtcNow;
        private SoftwareData _softwareData;
        private bool _alreadyNotifiedUserOfNoResponse = false;
        private bool _alreadyNotifiedUserOfNoPM = false;
        private bool _downloading = false;

        private bool _isOnline = true;
        private bool _isAuthenticated = true;
        private bool _hasJwt = true;

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

                // Create an AutoResetEvent to signal the timeout threshold in the
                // timer callback has been reached.
                var autoEvent = new AutoResetEvent(false);

                // setup timer to process events every 60 seconds
                long timerInterval = 60000L; // timer interval
                timer = new Timer(
                    ProcessSoftwareDataTimerCallbackAsync,
                    autoEvent,
                    timerInterval,
                    timerInterval);

                this.SendOfflineData();

                // start in 30 seconds
                kpmTimer = new Timer(
                    ProcessFetchDailyKpmTimerCallbackAsync,
                    autoEvent,
                    30000L,
                    timerInterval);

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

        private long getFileDiffLength(FileInfo fi)
        {
            String fileName = fi.FullName;

            long savedFileLen = _softwareData.getFileInfoDataForProperty(fileName, "length");

            long fileDiff = savedFileLen;

            if (fi != null)
            {
                fileDiff = fi.Length - savedFileLen;
            }
            return fileDiff;
        }

        private void DocEventsOnDocumentSaved(Document document)
        {
            String fileName = document.FullName;
            FileInfo fi = new FileInfo(fileName);
            long diff = this.getFileDiffLength(fi);
            long kpm = _softwareData.getFileInfoDataForProperty(fi.FullName, "add");
            if (diff > kpm + 1)
            {
                // register a copy and past event
                _softwareData.UpdateData(fileName, "paste", 1);
                Logger.Info("Software.com: Copy+Paste incremented");
            }
            _softwareData.UpdateData(fileName, "length", fi.Length);
        }

        private void DocEventsOnDocumentOpening(String docPath, Boolean readOnly)
        {
            FileInfo fi = new FileInfo(docPath);
            // update the length of this 
            if (_softwareData != null)
            {
                _softwareData.UpdateData(fi.FullName, "length", fi.Length);
            }
        }

        private void AfterKeyPressed(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
            InitializeSoftwareData();

            String fileName = ObjDte.ActiveWindow.Document.FullName;
            if (!String.IsNullOrEmpty(Keypress))
            {
                long fileLength = _softwareData.getFileInfoDataForProperty(fileName, "length");
                if (fileLength == 0)
                {
                    // update the file length
                    FileInfo fi = new FileInfo(fileName);
                    _softwareData.addOrUpdateFileInfo(fileName, "length", fi.Length);
                }

                if (Keypress == "\b")
                {
                    // register a delete event
                    _softwareData.UpdateData(fileName, "delete", 1);
                    Logger.Info("Software.com: Delete character incremented");
                }
                else
                {
                    _softwareData.UpdateData(fileName, "add", 1);
                    Logger.Info("Software.com: KPM incremented");
                }

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

        private long getNowInSeconds()
        {
            long nowMillis = Convert.ToInt64((DateTime.Now - DateTime.MinValue).TotalMilliseconds);
            return (long)Math.Round((double)(nowMillis / 1000));
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
                _softwareData.end = _softwareData.start + 60;

                string softwareDataContent = SimpleJson.SerializeObject(_softwareData);
                Logger.Info("Software.com: sending: " + softwareDataContent);

                /**
                 * [SoftwareCo Info 08:24:17 AM] Software.com: sending:
                 * {"type":"Events","pluginId":6,"source":{"C:\\Users\\Xavier Luiz\\source\\repos\\UnitTestProject3\\UnitTestProject3\\UnitTest1.cs":
                 * {"paste":0,"open":0,"close":0,"delete":7,"keys":17,"add":10,"netkeys":3,"length":595,"lines":0,"linesAdded":0,
                 * "linesRemoved":0,"syntax":""}},"data":"17","start":63665166199,"end":63665166259,
                 * "project":{"name":"UnitTestProject3","directory":"C:\\Users\\Xavier Luiz\\source\\repos"}}
                 **/

                HttpResponseMessage response = await SendRequest(HttpMethod.Post, "/data", softwareDataContent);
                if (!this.IsOk(response))
                {
                    this.StorePayload(softwareDataContent);
                }

                _softwareData.ResetData();
                _lastPostTime = now;
                _lastDocument = null;
            }
        }

        private bool IsOk(HttpResponseMessage response)
        {
            return (response != null && response.StatusCode == HttpStatusCode.OK);
        }

        private async Task<HttpResponseMessage> SendRequest(HttpMethod httpMethod, string uri, string optionalPayload)
        {

            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var cts = new CancellationTokenSource();
            HttpResponseMessage response = null;
            object jwt = this.getItem("jwt");
            if (jwt != null)
            {
                // add the authorizationn
                client.DefaultRequestHeaders.Add("Authorization", (string)jwt);
            }
            HttpContent contentPost = null;
            if (optionalPayload != null)
            {
                contentPost = new StringContent(optionalPayload, Encoding.UTF8, "application/json");
            }
            bool isPost = (httpMethod.Equals(HttpMethod.Post));
            try
            {
                string endpoint = Constants.api_endpoint + "" + uri;
                if (isPost)
                {
                    response = await client.PostAsync(endpoint, contentPost, cts.Token);
                }
                else
                {
                    response = await client.GetAsync(endpoint, cts.Token);
                }
            }
            catch (HttpRequestException e)
            {
                if (isPost)
                {
                    NotifyPostException(e);
                }
            }
            catch (TaskCanceledException e)
            {
                if (e.CancellationToken == cts.Token)
                {
                    // triggered by the caller
                    if (isPost)
                    {
                        NotifyPostException(e);
                    }
                }
                else
                {
                    // a web request timeout (possibly other things!?)
                    Logger.Info("We are having trouble receiving a response from Software.com");
                }
            }
            catch (Exception e)
            {
                if (isPost)
                {
                    NotifyPostException(e);
                }
            }
            finally
            {
            }
            return response;
        }

        private void NotifyPostException(Exception e)
        {
            Logger.Error("We are having trouble sending data to Software.com, reason: " + e.Message);
        }

        private void StorePayload(string softwareDataContent)
        {
            string datastoreFile = this.getSoftwareDataStoreFile();
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
            HttpResponseMessage response = await SendRequest(HttpMethod.Get, "/ping", null);
            this._isOnline = this.IsOk(response);
            this.UpdateStatus();
        }

        private async void IsAuthenticated()
        {
            HttpResponseMessage response = await SendRequest(HttpMethod.Get, "/users/ping", null);
            this._isAuthenticated = this.IsOk(response);
            this.UpdateStatus();
        }

        private bool HasJwt()
        {
            object jwt = this.getItem("jwt");
            this._hasJwt = (jwt != null);
            this.UpdateStatus();
            return this._hasJwt;
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

            object lastUpdateTime = this.getItem("vs_lastUpdateTime");
            long nowInSec = this.getNowInSeconds();
            if (lastUpdateTime != null && (nowInSec - (long)lastUpdateTime) < (60 * 60 * 24))
            {
                // we've already asked via the prompt. let the status bar do the work from now on
                return;
            }

            this.setItem("vs_lastUpdateTime", nowInSec);

            string msg = "To see your coding data in Software.com, please sign in to your account.";

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
                this.launchSoftwareDashboard();
            }
        }

        private async void SendOfflineData()
        {
            string datastoreFile = this.getSoftwareDataStoreFile();
            if (File.Exists(datastoreFile))
            {
                // get the content
                string[] lines = File.ReadAllLines(datastoreFile);

                if (lines != null && lines.Length > 0)
                {
                    List<String> jsonLines = new List<string>();
                    //StringBuilder sb = new StringBuilder();
                    foreach (string line in lines)
                    {
                        if (line != null && line.Trim().Length > 0)
                        {
                            //sb.Append(line).Append(",");
                            jsonLines.Add(line);
                        }
                    }
                    //String lineContent = sb.ToString();
                    //lineContent = lineContent.Substring(0, lineContent.LastIndexOf(","));
                    string jsonContent = "\\[" + string.Join(",", jsonLines) + "\\]";
                    HttpResponseMessage response = await SendRequest(HttpMethod.Post, "/data/batch", jsonContent);
                    if (this.IsOk(response))
                    {
                        // delete the file
                        File.Delete(datastoreFile);
                    }
                }
            }
        }

        private object getItem(string key)
        {
            // read the session json file
            string sessionFile = this.getSoftwareSessionFile();
            if (File.Exists(sessionFile))
            {
                string content = File.ReadAllText(sessionFile);
                if (content != null)
                {
                    object val = SimpleJson.GetValue(content, key);
                    if (val != null)
                    {
                        return val;
                    }
                }
            }
            return null;
        }

        private void setItem(String key, object val)
        {
            string sessionFile = this.getSoftwareSessionFile();
            IDictionary<string, object> dict = new Dictionary<string, object>();
            string content = "";
            if (File.Exists(sessionFile))
            {
                content = File.ReadAllText(sessionFile);
                // conver to dictionary
                dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content);
                dict.Remove(key);
            }
            dict.Add(key, val);
            content = SimpleJson.SerializeObject(dict);
            // write it back to the file
            File.WriteAllText(sessionFile, content);
        }

        private async void RetrieveAuthToken()
        {
            if (this.HasJwt())
            {
                return;
            }
            object token = this.getItem("token");
            string jwt = null;
            HttpResponseMessage response = await SendRequest(HttpMethod.Get, "/users/plugin/confirm?token=" + token, null);
            if (this.IsOk(response))
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                jsonObj.TryGetValue("jwt", out object jwtObj);
                jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);

                if (jwt != null)
                {
                    this.setItem("jwt", jwt);
                }

                this.setItem("vs_lastUpdateTime", this.getNowInSeconds());
            }

            if (jwt == null)
            {
                this.RetrieveAuthTokenTimeout(60000);
            }
        }

        private async void ProcessFetchDailyKpmTimerCallbackAsync(Object stateInfo)
        {
            if (this._isAuthenticated && this._isOnline)
            {
                long nowInSec = this.getNowInSeconds();
                HttpResponseMessage response = await SendRequest(HttpMethod.Get, "/sessions?summary=true&from=" + nowInSec, null);
                if (this.IsOk(response))
                {
                    // get the json data
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);

                    jsonObj.TryGetValue("inFlow", out object inFlowObj);
                    bool inFlow = (inFlowObj == null) ? true : Convert.ToBoolean(inFlowObj);
                    
                    jsonObj.TryGetValue("kpm", out object kpmObj);
                    long avgKpm = (kpmObj == null) ? 0 : Convert.ToInt64(kpmObj);
                    
                    jsonObj.TryGetValue("minutesTotal", out object minutesTotalObj);
                    long minutesTotal = (minutesTotalObj == null) ? 0 : Convert.ToInt64(minutesTotalObj);

                    string sessionTime = "";
                    if (minutesTotal == 60)
                    {
                        sessionTime = "1 hr";
                    }
                    else if (minutesTotal > 60)
                    {
                        sessionTime = String.Format("%.2f", (minutesTotal / 60)) + " hrs";
                    }
                    else if (minutesTotal == 1)
                    {
                        sessionTime = "1 min";
                    }
                    else
                    {
                        sessionTime = minutesTotal + " min";
                    }
                    
                    if (avgKpm > 0 || minutesTotal > 0)
                    {
                        this.SetStatus("<s> " + avgKpm + " KPM, " + sessionTime);
                    }
                    else
                    {
                        this.SetStatus("Software.com");
                    }
                }
                else
                {
                    this.CheckAuthStatus();
                }
            }
            else
            {
                this.CheckAuthStatus();
            }
            
        }

        private string createToken()
        {
            return System.Guid.NewGuid().ToString().Replace("-", "");
        }

        private void launchSoftwareDashboard()
        {
            string url = Constants.url_endpoint;
            if (!this.HasJwt())
            {
                // create the token
                string tokenVal = this.createToken();
                this.setItem("token", tokenVal);
                url += "/login?token=" + tokenVal;
            }

            System.Diagnostics.Process.Start(url);

            this.RetrieveAuthTokenTimeout(30000);
        }

        private async void RetrieveAuthTokenTimeout(int millisToWait)
        {
            if (!this.HasJwt())
            {
                await Task.Delay(millisToWait);
                this.RetrieveAuthToken();
            }
        }

        private bool EnoughTimePassed(DateTime now)
        {
            return _lastPostTime < now.AddMinutes(postFrequency);
        }

        private void InitializeSoftwareData()
        {
            String projectName = (ObjDte.Solution != null) ? Path.GetFileNameWithoutExtension(ObjDte.Solution.FullName) : "None";
            String directoryName = (ObjDte.Solution != null) ? ObjDte.Solution.FileName : "";
            int firstIdx = directoryName.IndexOf(projectName);
            if (firstIdx > 0)
            {
                directoryName = directoryName.Substring(0, firstIdx - 1);
            }

            if (_softwareData == null)
            {
                ProjectInfo projectInfo = new ProjectInfo(projectName, directoryName);
                _softwareData = new SoftwareData(projectInfo);
            }
            else if (String.IsNullOrEmpty(_softwareData.project.directory) ||
                String.IsNullOrEmpty(_softwareData.project.name) ||
                _softwareData.project.name.Equals("None"))
            {
                _softwareData.project.name = projectName;
                _softwareData.project.directory = directoryName;
            }
        }

        private bool IsPluginManagerInstalled()
        {
            string appDataProgramsPath = this.getPluginManagerInstallDirectory();
            string appDataRoamingPath = this.getPluginAppDataRoamingInstallDirectory();
            if (File.Exists(appDataProgramsPath))
            {
                string[] files = Directory.GetFiles(appDataProgramsPath);
                foreach (string file in files)
                {
                    if (!String.IsNullOrEmpty(file) && file.ToLower().StartsWith("software"))
                    {
                        return true;
                    }
                }
            }
            else if (File.Exists(appDataRoamingPath))
            {
                string[] files = Directory.GetFiles(appDataRoamingPath);
                foreach (string file in files)
                {
                    if (!String.IsNullOrEmpty(file) && file.ToLower().StartsWith("preferences"))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private String getDownloadDestinationDirectory()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private String getDownloadDestinationPathName()
        {
            return this.getDownloadDestinationDirectory() + "\\Software.exe";
        }

        private String getPluginManagerFileUrl()
        {
            return "https://s3-us-west-1.amazonaws.com/swdc-plugin-manager/software.exe";
        }

        private String getSoftwareDataDir()
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (!Directory.Exists(softwareDataDir))
            {
                // create it
                Directory.CreateDirectory(softwareDataDir);
            }
            return softwareDataDir;
        }

        private String getSoftwareSessionFile()
        {
            return this.getSoftwareDataDir() + "\\session.json";
        }

        private String getSoftwareDataStoreFile()
        {
            return this.getSoftwareDataDir() + "\\data.json";
        }

        private String getPluginManagerInstallDirectory()
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            return userHomeDir + "\\AppData\\Local\\Programs\\softwarecom-plugin-manager";
        }

        private String getPluginAppDataRoamingInstallDirectory()
        {
            String appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return appDataFolder + "\\softwarecom-plugin-manager";
        }

        private async void DownloadPluginManager()
        {
            _downloading = true;
            try
            {
                string pmUrl = this.getPluginManagerFileUrl();
                string pmFileToSave = this.getDownloadDestinationPathName();

                WebClient client = new WebClient();
                this.SetStatus("Downloading Software desktop...");
                client.DownloadFile(pmUrl, pmFileToSave);
                this.SetStatus("Completed Software desktop download");
                await Task.Delay(5000);
                // install it
                System.Diagnostics.Process.Start(pmFileToSave);
            }
            finally
            {
                _downloading = false;
            }

            /**
            Uri uri = new Uri(pmFileToSave);

            // Specify that the DownloadFileCallback method gets called
            // when the download completes
            client.DownloadFileCompleted += new AsyncCompletedEventHandler(DownloadProgressCompletedCallback);
            // Specify a progress notification handler.
            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
            client.DownloadFileAsync(uri, pmFileToSave);
            **/
            }

            private void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            this.SetStatus("Downloading Software desktop: " + e.ProgressPercentage + "%");
        }

        private void DownloadProgressCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            this.SetStatus("Completed Software desktop download");
        }

        private void SetStatus(string msg)
        {
            IVsStatusbar statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusbar.SetText(msg);
        }

        #endregion
    }
}
