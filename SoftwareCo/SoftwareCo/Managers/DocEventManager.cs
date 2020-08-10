﻿using System;
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
using Microsoft.VisualStudio.Threading;

namespace SoftwareCo
{
    public sealed class DocEventManager
    {
        private static readonly Lazy<DocEventManager> lazy = new Lazy<DocEventManager>(() => new DocEventManager());

        // private SoftwareData _softwareData;
        private PluginData _pluginData;
        // Used by Constants for version info
        public static DTE2 ObjDte { set; get; }
        public static string _solutionDirectory { get; set; }

        private static int THIRTY_SECONDS = 1000 * 30;
        private static int ONE_MINUTE = THIRTY_SECONDS * 2;
        private static int ZERO_SECOND = 1;

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
                _solutionDirectory = await ProjectManager.GetSolutionDirectory();
                if (_solutionDirectory != null && !_solutionDirectory.Equals(""))
                {
                    FileInfo fi = new FileInfo(_solutionDirectory);
                    _pluginData = new PluginData(fi.Name, _solutionDirectory);
                } else
                {
                    // set it to unnamed
                    _pluginData = new PluginData("Unnamed", "Untitled");
                }
                // SoftwareCoUtil.SetTimeout(ONE_MINUTE, PostData, false);
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
            String fileName = ObjDte.ActiveWindow.Document.FullName;
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);

            PluginDataFileInfo pdfileInfo = _pluginData.GetFileInfo(fileName);
            if (ObjDte.ActiveWindow.Document.Language != null)
            {
                pdfileInfo.syntax = ObjDte.ActiveWindow.Document.Language;
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

            TrackerUtilManager.TrackEditorFileActionEvent("file", "open", fileName);

            try
            {
                _pluginData.GetFileInfo(fileName).open += 1;
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
            if (!IsTrueEventFile(fileName))
            {
                return;
            }
            InitPluginDataIfNotExists();
            _pluginData.InitFileInfoIfNotExists(fileName);

            TrackerUtilManager.TrackEditorFileActionEvent("file", "close", fileName);
        }

        
        public async void PostData()
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            DateTime now = DateTime.UtcNow;

            if (_pluginData != null && _pluginData.source.Count > 0 && _pluginData.keystrokes > 0)
            {
                // create the aggregates, end the file times, gather the cumulatives
                await _pluginData.CompletePayload();

                TrackerUtilManager.TrackCodeTimeEventAsync(_pluginData);

                string softwareDataContent = _pluginData.JsonStringify();

                Logger.Info("Code Time: storing plugin data: " + softwareDataContent);

                FileManager.AppendPluginData(softwareDataContent);
            }

            // update the latestPayloadTimestampEndUtc
            FileManager.setNumericItem("latestPayloadTimestampEndUtc", nowTime.now);

            // update the status bar and tree
            WallclockManager.Instance.DispatchUpdateAsync();
            _pluginData = null;
        }
    }
}
