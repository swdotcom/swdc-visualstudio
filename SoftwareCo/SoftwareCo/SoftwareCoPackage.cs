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
                long delayToStart = 1000L; // wait a second to start
                long timerInterval = 60000L; // timer interval
                timer = new Timer(
                    ProcessSoftwareDataTimerCallbackAsync,
                    autoEvent,
                    delayToStart,
                    timerInterval);
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
            long kpm = _softwareData.getFileInfoDataForProperty(fi.FullName, "keys");
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
                    _softwareData.UpdateData(fileName, "keys", 1);
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
                long nowInSec = this.getNowInSeconds();
                _softwareData.start = nowInSec - 60;
                _softwareData.end = nowInSec;

                string softwareDataContent = SimpleJson.SerializeObject(_softwareData);
                Logger.Info("Software.com: sending: " + softwareDataContent);
                HttpContent contentPost = new StringContent(softwareDataContent, Encoding.UTF8, "application/json");

                HttpClient client = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(5)
                };
                var cts = new CancellationTokenSource();
                HttpResponseMessage response = null;
                try
                {
                    response = await client.PostAsync(PLUGIN_MGR_ENDPOINT, contentPost, cts.Token);
                }
                catch (HttpRequestException e)
                {
                    NotifyPostException(e);
                }
                catch (TaskCanceledException e)
                {
                    if (e.CancellationToken == cts.Token)
                    {
                        // triggered by the caller
                        NotifyPostException(e);
                    }
                    else
                    {
                        // a web request timeout (possibly other things!?)
                        if (!_alreadyNotifiedUserOfNoResponse)
                        {
                            _alreadyNotifiedUserOfNoResponse = true;
                            Logger.Info("" +
                                "We are having trouble receiving a response from Software.com. " +
                                "Please make sure the Plugin Manager is running and logged in.");
                        }
                    }
                }
                catch (Exception e)
                {
                    NotifyPostException(e);
                }
                finally
                {
                    _softwareData.ResetData();
                    _lastPostTime = now;
                    _lastDocument = null;
                }
            }
        }

        private void NotifyPostException(Exception e)
        {

            if (!_alreadyNotifiedUserOfNoPM && !this.IsPluginManagerInstalled())
            {
                IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

                _alreadyNotifiedUserOfNoPM = true;

                string msg = "We are having trouble sending data to Software.com. " +
                "The Software plugin manager may not be installed.  Would you like to download it now?";

                Guid clsid = Guid.Empty;
                int result;
                uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    string.Empty,
                    msg,
                    string.Empty,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_YESNO,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO,
                    0,
                    out result);
                // yes = 6, no = 7
                if (result == 6)
                {
                    this.DownloadPluginManager();
                }
            } else if (!_downloading && !_alreadyNotifiedUserOfNoResponse)
            {
                _alreadyNotifiedUserOfNoResponse = true;

                IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));

                string msg = "We are having trouble sending data to Software.com. " +
                "Please make sure the Plugin Manager is on and logged in.";

                Guid clsid = Guid.Empty;
                int result;
                uiShell.ShowMessageBox(
                    0,
                    ref clsid,
                    string.Empty,
                    msg,
                    string.Empty,
                    0,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                    OLEMSGICON.OLEMSGICON_INFO,
                    0,
                    out result);
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
                IVsStatusbar statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
                statusbar.SetText("Downloading Software plugin manager...");
                client.DownloadFile(pmUrl, pmFileToSave);
                statusbar.SetText("Completed Software plugin manager download");
                await Task.Delay(5000);
                statusbar.Clear();
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
            IVsStatusbar statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusbar.SetText("Downloading Software plugin manager: " + e.ProgressPercentage + "%");
        }

        private void DownloadProgressCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            IVsStatusbar statusbar = GetService(typeof(SVsStatusbar)) as IVsStatusbar;
            statusbar.SetText("Completed Software plugin manager download");
        }

        #endregion
    }
}
