
using System;
using EnvDTE;
using System.IO;

namespace SoftwareCo
{
    public sealed class DocEventManager
    {
        private static readonly Lazy<DocEventManager> lazy = new Lazy<DocEventManager>(() => new DocEventManager());

        // private SoftwareData _softwareData;
        private PluginData _pluginData;

        private SessionSummaryManager sessionSummaryMgr;

        public static DocEventManager Instance { get { return lazy.Value; } }

        private DocEventManager()
        {
            sessionSummaryMgr = SessionSummaryManager.Instance;
        }

        public bool hasData()
        {
            if (_pluginData != null && _pluginData.source != null && _pluginData.source.Count > 0 && _pluginData.keystrokes > 0)
            {
                return true;
            }
            return false;
        }

        private bool IsTrueEventFile(string fileName)
        {
            return (fileName == null || fileName.IndexOf("CodeTime.txt") != -1) ? false : true;
        }

        private async void InitPluginDataIfNotExists()
        {
            if (_pluginData == null)
            {
                string _solutionDirectory = await PackageManager.GetSolutionDirectory();
                if (_solutionDirectory != null && !_solutionDirectory.Equals(""))
                {
                    FileInfo fi = new FileInfo(_solutionDirectory);
                    _pluginData = new PluginData(fi.Name, _solutionDirectory);
                } else
                {
                    // set it to unnamed
                    _pluginData = new PluginData("Unnamed", "Untitled");
                }
            }
        }

        public void DocEventsOnDocumentSaved(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);


            // wrapper for a file path
            FileInfo fi = new FileInfo(fileName);
            _pluginData.GetFileInfo(fileName).length = fi.Length;
        }

        public async void DocEventsOnDocumentOpeningAsync(String docPath, Boolean readOnly)
        {
            // wrapper for a file path
            FileInfo fi = new FileInfo(docPath);
            String fileName = fi.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);
        }

        public async void AfterKeyPressedAsync(
            string Keypress, TextSelection Selection, bool InStatementCompletion)
        {
            String fileName = await PackageManager.GetActiveDocumentFileName();
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);

            PluginDataFileInfo pdfileInfo = _pluginData.GetFileInfo(fileName);
            if (string.IsNullOrEmpty(pdfileInfo.syntax))
            {
                string syntax = await PackageManager.GetActiveDocumentSyntax();

                if (!string.IsNullOrEmpty(syntax))
                {
                    pdfileInfo.syntax = syntax;
                }
            }

            if (!String.IsNullOrEmpty(Keypress))
            {
                FileInfo fi = new FileInfo(fileName);

                bool isNewLine = false;
                if (Keypress == "\b")
                {
                    // register a delete event
                    pdfileInfo.delete += 1;
                    Logger.Info("Code Time: Delete character incremented");
                }
                else if (Keypress == "\r")
                {
                    isNewLine = true;
                }
                else
                {
                    pdfileInfo.add += 1;
                    Logger.Info("Code Time: KPM incremented");
                }

                if (isNewLine)
                {
                    pdfileInfo.linesAdded += 1;
                }
                // file level keystrokes counter
                pdfileInfo.keystrokes += 1;
                // top level keystrokes counter
                _pluginData.keystrokes += 1;
            }
        }

        public async void DocEventsOnDocumentOpenedAsync(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);

            TrackerEventManager.TrackEditorFileActionEvent("file", "open", fileName);
        }

        public async void DocEventsOnDocumentClosedAsync(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            String fileName = document.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);

            TrackerEventManager.TrackEditorFileActionEvent("file", "open", fileName);
        }

        
        public async void PostData()
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();

            if (_pluginData != null && _pluginData.source.Count > 0 && _pluginData.keystrokes > 0)
            {

                // create the aggregates, end the file times, gather the cumulatives
                string softwareDataContent = await _pluginData.CompletePayloadAndReturnJsonString();

                Logger.Info("Code Time: storing plugin data: " + softwareDataContent);

                TrackerEventManager.TrackCodeTimeEventAsync(_pluginData);

                FileManager.AppendPluginData(softwareDataContent);

                // update the latestPayloadTimestampEndUtc
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

                // update the status bar and tree
                WallclockManager.Instance.DispatchUpdateAsync();
                _pluginData = null;
            }

            
        }
    }
}
