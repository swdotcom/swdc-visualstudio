

using EnvDTE;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace SoftwareCo
{
    public sealed class DocEventManager
    {
        private static readonly Lazy<DocEventManager> lazy = new Lazy<DocEventManager>(() => new DocEventManager());

        // private SoftwareData _softwareData;
        private PluginData _pluginData;
        private Document doc = null;
        private Scheduler scheduler = null;
        private string currentSolutionDirectory = "";
        private static int ACTIVITY_LAG_THRESHOLD_SEC = 30;
        public static DocEventManager Instance { get { return lazy.Value; } }

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
            return (string.IsNullOrEmpty(fileName) || fileName.IndexOf("CodeTime.txt") != -1) ? false : true;
        }

        private void InitPluginDataIfNotExists(string fileName)
        {
            if (_pluginData == null && doc != null && !string.IsNullOrEmpty(fileName))
            {
                if (_pluginData == null)
                {
                    // initialize the 1-minute timer
                    new Scheduler().Execute(() => PostData(), 1000 * 60);
                }
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (!string.IsNullOrEmpty(currentSolutionDirectory))
                {
                    FileInfo fi = new FileInfo(currentSolutionDirectory);
                    _pluginData = new PluginData(fi.Name, currentSolutionDirectory);
                }
                else
                {
                    // set it to unnamed
                    _pluginData = new PluginData("Unnamed", "Untitled");
                }

                _pluginData.InitFileInfoIfNotExists(fileName);
                long elapsedTimeInMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds() - now;
                if (elapsedTimeInMillis > 5)
                {
                    Logger.Info("Initialized plugin data file, elapsed: " + elapsedTimeInMillis + " ms");
                }
            }
        }

        public static int CountLinesLINQ(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            if (fi != null && fi.Exists)
            {
                return File.ReadLines(fi.FullName).Count();
            }
            return 0;
        }

        public async void LineChangedAsync(TextPoint start, TextPoint end, int hint)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (doc == null)
            {
                return;
            }

            string fileName = doc.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }

            SoftwareCoPackage package = PackageManager.GetAsyncPackage();
            if (package != null)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            // only allow single line or bulk paste. visual sudio marks up the document
            // when opening the file so we need to prevent these change events from coming through
            bool isSameLinePaste = start.Line == end.Line && start.LineCharOffset < start.LineLength && end.LineCharOffset > start.LineCharOffset;
            bool isBulkPaste = start.AtStartOfLine && end.AtEndOfLine && !end.AtStartOfLine && start.Line < end.Line;
            if (isSameLinePaste || isBulkPaste)
            {
                if (string.IsNullOrEmpty(currentSolutionDirectory))
                {
                    currentSolutionDirectory = await PackageManager.GetSolutionDirectory();
                }

                if (_pluginData == null || _pluginData.GetFileInfo(fileName) == null)
                {
                    InitPluginDataIfNotExists(fileName);
                }

                PluginDataFileInfo pdfileInfo = _pluginData.GetFileInfo(fileName);

                if (pdfileInfo == null)
                {
                    return;
                }

                UpdateFileInfoMetrics(pdfileInfo, start, end, null, null);
                long elapsedTimeInMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds() - now;
                if (elapsedTimeInMillis > 10)
                {
                    Logger.Info("LineChanged elapsed: " + elapsedTimeInMillis + " ms");
                }
            }
        }

        public async void BeforeKeyPressAsync(string Keypress, TextSelection Selection, bool InStatementCompletion, bool CancelKeypress)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (doc == null)
            {
                return;
            }

            string fileName = doc.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }

            SoftwareCoPackage package = PackageManager.GetAsyncPackage();
            if (package != null)
            {
                await package.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            if (string.IsNullOrEmpty(currentSolutionDirectory))
            {
                currentSolutionDirectory = await PackageManager.GetSolutionDirectory();
            }

            if (_pluginData == null || _pluginData.GetFileInfo(fileName) == null)
            {
                InitPluginDataIfNotExists(fileName);
            }

            PluginDataFileInfo pdfileInfo = _pluginData.GetFileInfo(fileName);
            if (pdfileInfo == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(pdfileInfo.syntax))
            {
                string syntax = doc.Language;

                if (!string.IsNullOrEmpty(syntax))
                {
                    pdfileInfo.syntax = syntax;
                }
            }

            UpdateFileInfoMetrics(pdfileInfo, null, null, Keypress, Selection);
            long elapsedTimeInMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds() - now;
            if (elapsedTimeInMillis > 10)
            {
                Logger.Info("BeforeKeyPressed elapsed: " + elapsedTimeInMillis + " ms");
            }
        }

        public async void WindowVisibilityEventAsync(Window Window)
        {
            if (Window != null && Window.Document != null)
            {
                string fileName = Window.Document.FullName;
                doc = Window.Document;
                if (string.IsNullOrEmpty(currentSolutionDirectory))
                {
                    currentSolutionDirectory = await PackageManager.GetSolutionDirectory();
                }
                if (_pluginData == null || _pluginData.GetFileInfo(fileName) == null)
                {
                    InitPluginDataIfNotExists(fileName);
                }
                TrackerEventManager.TrackEditorFileActionEvent("file", "open", fileName);
            }
        }

        public async void DocEventsOnDocumentClosedAsync(Document document)
        {
            if (document == null || document.FullName == null)
            {
                return;
            }
            string fileName = document.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }

            TrackerEventManager.TrackEditorFileActionEvent("file", "close", fileName);
        }

        private async void UpdateFileInfoMetrics(PluginDataFileInfo fileInfo, TextPoint start, TextPoint end, string Keypress, TextSelection textSelection)
        {
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            bool hasAutoIndent = false;
            bool newLineAutoIndent = false;

            int numKeystrokes = 0;
            int numDeleteKeystrokes = 0;

            int linesRemoved = 0;
            int linesAdded = 0;

            if (start != null && end != null)
            {
                // this is a line change event
                linesAdded = end.Line - start.Line;
                numKeystrokes = end.AbsoluteCharOffset - start.AbsoluteCharOffset;
            }
            else if (Keypress != null)
            {
                if (textSelection != null)
                {
                    linesRemoved = textSelection.BottomLine > textSelection.TopLine ? textSelection.BottomLine - textSelection.TopLine : 0;
                    linesAdded = textSelection.TopLine > textSelection.BottomLine ? textSelection.TopLine - textSelection.BottomLine : 0;

                    if (linesRemoved > 0)
                    {
                        numDeleteKeystrokes = textSelection.BottomPoint.AbsoluteCharOffset - textSelection.TopPoint.AbsoluteCharOffset;
                    }
                }

                if (Keypress == "\b" && numDeleteKeystrokes == 0)
                {
                    // it's a single delete
                    numDeleteKeystrokes = 1;
                }
                else if (linesRemoved == 0 && textSelection != null && textSelection.CurrentColumn == 1 && Keypress == "\b")
                {
                    // it's a single line delete
                    linesRemoved = 1;
                }
                else if (Keypress == "\r" && linesAdded == 0)
                {
                    // it's a single carriage return
                    linesAdded = 1;
                    if (textSelection.CurrentColumn > 1)
                    {
                        hasAutoIndent = true;
                    }
                }
                else if (Keypress == "\t")
                {
                    hasAutoIndent = true;
                }
                else if (numDeleteKeystrokes == 0)
                {
                    // it's a single character
                    numKeystrokes = 1;
                }
            }

            // update the deletion keystrokes if there are lines removed
            numDeleteKeystrokes = numDeleteKeystrokes >= linesRemoved ? numDeleteKeystrokes - linesRemoved : numDeleteKeystrokes;

            // Logger.Info("{la: " + linesAdded + ", lr: " + linesRemoved + ", dk: " + numDeleteKeystrokes + ", k: " + numKeystrokes + ", indent: " + hasAutoIndent + "}");

            // event updates
            if (newLineAutoIndent)
            {
                // it's a new line with auto-indent
                fileInfo.auto_indents += 1;
                fileInfo.linesAdded += 1;
            }
            else if (hasAutoIndent)
            {
                // it's an auto indent action
                fileInfo.auto_indents += 1;
                if (linesAdded > 0)
                {
                    fileInfo.linesAdded += 1;
                    fileInfo.single_adds += 1;
                }
            }
            else if (linesAdded == 1)
            {
                // it's a single new line action (single_adds)
                fileInfo.single_adds += 1;
                fileInfo.linesAdded += 1;
            }
            else if (linesAdded > 1)
            {
                // it's a multi line paste action (multi_adds)
                fileInfo.linesAdded += linesAdded;
                fileInfo.paste += 1;
                fileInfo.multi_adds += 1;
                fileInfo.is_net_change = true;
                fileInfo.characters_added += Math.Abs(numKeystrokes - linesAdded);
            }
            else if (numDeleteKeystrokes > 0 && numKeystrokes > 0)
            {
                // it's a replacement
                fileInfo.replacements += 1;
                fileInfo.characters_added += numKeystrokes;
                fileInfo.characters_deleted += numDeleteKeystrokes;
            }
            else if (numKeystrokes > 1)
            {
                // pasted characters (multi_adds)
                fileInfo.paste += 1;
                fileInfo.multi_adds += 1;
                fileInfo.is_net_change = true;
                fileInfo.characters_added += numKeystrokes;
            }
            else if (numKeystrokes == 1)
            {
                // it's a single keystroke action (single_adds)
                fileInfo.add += 1;
                fileInfo.single_adds += 1;
                fileInfo.characters_added += 1;
            }
            else if (linesRemoved == 1)
            {
                // it's a single line deletion
                fileInfo.linesRemoved += 1;
                fileInfo.single_deletes += 1;
                fileInfo.characters_deleted += numDeleteKeystrokes;
            }
            else if (linesRemoved > 1)
            {
                // it's a multi line deletion and may contain characters
                fileInfo.characters_deleted += numDeleteKeystrokes;
                fileInfo.multi_deletes += 1;
                fileInfo.is_net_change = true;
                fileInfo.linesRemoved += linesRemoved;
            }
            else if (numDeleteKeystrokes == 1)
            {
                // it's a single character deletion action
                fileInfo.delete += 1;
                fileInfo.single_deletes += 1;
                fileInfo.characters_deleted += 1;
            }
            else if (numDeleteKeystrokes > 1)
            {
                // it's a multi character deletion action
                fileInfo.multi_deletes += 1;
                fileInfo.is_net_change = true;
                fileInfo.characters_deleted += numDeleteKeystrokes;
            }

            if (linesAdded > 0)
            {
                fileInfo.lines += linesAdded;
            }
            else if (linesRemoved > 0)
            {
                fileInfo.lines -= linesRemoved;
            }

            fileInfo.keystrokes += 1;
            _pluginData.keystrokes += 1;

            long elapsedTimeInMillis = DateTimeOffset.Now.ToUnixTimeMilliseconds() - now;
            if (elapsedTimeInMillis > 10)
            {
                Logger.Info("Updated file info metrics, elapsed: " + elapsedTimeInMillis + " ms");
            }
        }

        public async void PostData()
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();

            if (hasData())
            {
                // create the aggregates, end the file times, gather the cumulatives
                string softwareDataContent = await _pluginData.CompletePayloadAndReturnJsonString();

                Logger.Info("Code Time: processing plugin data: " + softwareDataContent);

                TrackerEventManager.TrackCodeTimeEventAsync(_pluginData);

                FileManager.AppendPluginData(softwareDataContent);

                // update the latestPayloadTimestampEndUtc
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

                _pluginData = null;
            }
        }
    }
}
