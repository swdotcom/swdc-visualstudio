using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;

namespace SoftwareCo
{
    public sealed class DocEventManager
    {
        private static readonly Lazy<DocEventManager> lazy = new Lazy<DocEventManager>(() => new DocEventManager());

        private SoftwareData _softwareData;
        // Used by Constants for version info
        public static DTE2 ObjDte { set; get; }

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ZERO_SECOND = 1;

        private SessionSummaryManager sessionSummaryMgr;

        public static DocEventManager Instance { get { return lazy.Value; } }

        private DocEventManager()
        {
            sessionSummaryMgr = SessionSummaryManager.Instance;
        }

        public void DocEventsOnDocumentSaved(Document document)
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
            // wrapper for a file path
            FileInfo fi = new FileInfo(fileName);

            _softwareData.UpdateData(fileName, "length", fi.Length);
        }

        public async void DocEventsOnDocumentOpeningAsync(String docPath, Boolean readOnly)
        {
            // wrapper for a file path
            FileInfo fi = new FileInfo(docPath);
            String fileName = fi.FullName;
            InitializeSoftwareData(fileName);

            //Sets end and local_end for source file
            await InitializeFileInfo(fileName);
        }

        public async void AfterKeyPressedAsync(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
            String fileName = ObjDte.ActiveWindow.Document.FullName;
            InitializeSoftwareData(fileName);

            //Sets end and local_end for source file
            await InitializeFileInfo(fileName);

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

        public async void DocEventsOnDocumentOpenedAsync(Document document)
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
            await InitializeFileInfo(fileName);
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

        public async void DocEventsOnDocumentClosedAsync(Document document)
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
            await InitializeFileInfo(fileName);
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

        public void InitializeSoftwareData(string fileName)
        {
            string MethodName = "InitializeSoftwareData";
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
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
                _softwareData.start = nowTime.now;
                _softwareData.local_start = nowTime.local_now;
                _softwareData.initialized = true;
                SoftwareCoUtil.SetTimeout(ONE_MINUTE, HasData, false);
            }
            _softwareData.EnsureFileInfoDataIsPresent(fileName, nowTime);
        }

        private async Task InitializeFileInfo(string fileName)
        {

            JsonObject localSource = new JsonObject();
            foreach (var sourceFiles in _softwareData.source)
            {
                object outend = null;
                JsonObject fileInfoData = null;
                NowTime nowTime = SoftwareCoUtil.GetNowTime();

                if (fileName != sourceFiles.Key)
                {
                    fileInfoData = (JsonObject)sourceFiles.Value;
                    fileInfoData.TryGetValue("end", out outend);

                    if (long.Parse(outend.ToString()) == 0)
                    {

                        fileInfoData["end"] = nowTime.now;
                        fileInfoData["local_end"] = nowTime.local_now;

                    }
                    localSource.Add(sourceFiles.Key, fileInfoData);
                }
                else
                {
                    fileInfoData = (JsonObject)sourceFiles.Value;
                    fileInfoData["end"] = 0;
                    fileInfoData["local_end"] = 0;
                    localSource.Add(sourceFiles.Key, fileInfoData);
                }

                _softwareData.source = localSource;

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
            double offset = 0;
            long end = 0;
            long local_end = 0;

            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            DateTime now = DateTime.UtcNow;
            if (_softwareData.source.Count > 0)
            {
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

                }

                try
                {
                    _softwareData.end = nowTime.now;
                    _softwareData.local_end = nowTime.local_now;
                }
                catch (Exception) { }

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
            }

        }

        private void StorePayload(SoftwareData _softwareData)
        {
            if (_softwareData != null)
            {

                long keystrokes = _softwareData.keystrokes;

                UpdateAggregates();

                string softwareDataContent = _softwareData.GetAsJson();

                string datastoreFile = SoftwareCoUtil.getSoftwareDataStoreFile();
                // append to the file
                File.AppendAllText(datastoreFile, softwareDataContent + Environment.NewLine);
            }
        }

        private void UpdateAggregates()
        {
            List<FileInfoSummary> fileInfoList = _softwareData.GetSourceFileInfoList();
            KeystrokeAggregates aggregates = new KeystrokeAggregates();
            if (_softwareData.project != null)
            {
                aggregates.directory = _softwareData.project.directory;
            } else
            {
                aggregates.directory = "Unamed";
            }

            foreach (FileInfoSummary fileInfo in fileInfoList)
            {
                aggregates.Aggregate(fileInfo);
            }

            sessionSummaryMgr.IncrementSessionSummaryData(aggregates);
        }
    }
}
