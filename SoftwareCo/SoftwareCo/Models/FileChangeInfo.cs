﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class FileChangeInfo
    {
        public long add { get; set; }
        public long close { get; set; }
        public long delete { get; set; }
        public long linesAdded { get; set; }
        public long linesRemoved { get; set; }
        public long charsPasted { get; set; }
        public long open { get; set; }
        public long paste { get; set; }
        public long keystrokes { get; set; }
        public long netkeys { get; set; }
        public string syntax { get; set; }
        public long start { get; set; }
        public long end { get; set; }
        public long local_start { get; set; }
        public long local_end { get; set; }
        public long duration_seconds { get; set; }
        public string projectDir { get; set; }
        public string fsPath { get; set; }
        public string name { get; set; }
        public long update_count { get; set; }
        public Int32 fileAgeDays { get; set; }
        public Int32 repoFileContributorCount { get; set; }

        public void CloneFromDictionary(IDictionary<string, object> dict)
        {
            this.fsPath = SoftwareCoUtil.ConvertObjectToString(dict, "fsPath");
            this.name = SoftwareCoUtil.ConvertObjectToString(dict, "name");
            this.add = SoftwareCoUtil.ConvertObjectToLong(dict, "add");
            this.close = SoftwareCoUtil.ConvertObjectToLong(dict, "close");
            this.delete = SoftwareCoUtil.ConvertObjectToLong(dict, "delete");
            this.linesAdded = SoftwareCoUtil.ConvertObjectToLong(dict, "linesAdded");
            this.linesRemoved = SoftwareCoUtil.ConvertObjectToLong(dict, "linesRemoved");
            this.charsPasted = SoftwareCoUtil.ConvertObjectToLong(dict, "charsPasted");
            this.open = SoftwareCoUtil.ConvertObjectToLong(dict, "open");
            this.paste = SoftwareCoUtil.ConvertObjectToLong(dict, "paste");
            this.keystrokes = SoftwareCoUtil.ConvertObjectToLong(dict, "keystrokes");
            this.netkeys = SoftwareCoUtil.ConvertObjectToLong(dict, "netkeys");
            this.syntax = SoftwareCoUtil.ConvertObjectToString(dict, "syntax");
            this.start = SoftwareCoUtil.ConvertObjectToLong(dict, "start");
            this.local_start = SoftwareCoUtil.ConvertObjectToLong(dict, "local_start");
            this.local_end = SoftwareCoUtil.ConvertObjectToLong(dict, "local_end");
            this.duration_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "duration_seconds");
            this.projectDir = SoftwareCoUtil.ConvertObjectToString(dict, "projectDir");
            this.update_count = SoftwareCoUtil.ConvertObjectToLong(dict, "update_count");
            this.fileAgeDays = SoftwareCoUtil.ConvertObjectToInt(dict, "fileAgeDays");
            this.repoFileContributorCount = SoftwareCoUtil.ConvertObjectToInt(dict, "repoFileContributorCount");
            this.UpdateName();
        }

        public void Clone(FileChangeInfo info)
        {
            this.add = info.add;
            this.close = info.close;
            this.delete = info.delete;
            this.linesAdded = info.linesAdded;
            this.linesRemoved = info.linesRemoved;
            this.charsPasted = info.charsPasted;
            this.open = info.open;
            this.paste = info.paste;
            this.keystrokes = info.keystrokes;
            this.netkeys = info.netkeys;
            this.syntax = info.syntax;
            this.start = info.start;
            this.end = info.end;
            this.local_start = info.local_start;
            this.local_end = info.local_end;
            this.duration_seconds = info.duration_seconds;
            this.projectDir = info.projectDir;
            this.fsPath = info.fsPath;
            this.name = info.name;
            this.update_count = info.update_count;
            this.fileAgeDays = info.fileAgeDays;
            this.repoFileContributorCount = info.repoFileContributorCount;
            this.UpdateName();
        }

        public void UpdateFromFileInfo(FileInfoSummary fileInfo)
        {
            this.add += fileInfo.add;
            this.close += fileInfo.close;
            this.delete += fileInfo.delete;
            this.linesAdded += fileInfo.linesAdded;
            this.linesRemoved += fileInfo.linesRemoved;
            this.charsPasted += fileInfo.charsPasted;
            this.open += fileInfo.open;
            this.paste += fileInfo.paste;
            this.keystrokes += fileInfo.keystrokes;
            this.netkeys += fileInfo.netkeys;
            this.syntax = fileInfo.syntax;
            this.duration_seconds += fileInfo.end - fileInfo.start;
            this.fsPath = fileInfo.fsPath;
            this.update_count += 1;
            this.UpdateName();
        }

        public JsonObject GetAsJson()
        {
            this.UpdateName();
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("add", this.add);
            jsonObj.Add("close", this.close);
            jsonObj.Add("delete", this.delete);
            jsonObj.Add("linesAdded", this.linesAdded);
            jsonObj.Add("linesRemoved", this.linesRemoved);
            jsonObj.Add("charsPasted", this.charsPasted);
            jsonObj.Add("open", this.open);
            jsonObj.Add("paste", this.paste);
            jsonObj.Add("keystrokes", this.keystrokes);
            jsonObj.Add("netkeys", this.netkeys);
            jsonObj.Add("syntax", this.syntax);
            jsonObj.Add("start", this.start);
            jsonObj.Add("end", this.end);
            jsonObj.Add("local_start", this.local_start);
            jsonObj.Add("local_end", this.local_end);
            jsonObj.Add("duration_seconds", this.duration_seconds);
            jsonObj.Add("projectDir", this.projectDir);
            jsonObj.Add("fsPath", this.fsPath);
            jsonObj.Add("name", this.name);
            jsonObj.Add("update_count", this.update_count);
            jsonObj.Add("fileAgeDays", this.fileAgeDays);
            jsonObj.Add("repoFileContributorCount", this.repoFileContributorCount);
            return jsonObj;
        }

        private void UpdateName()
        {
            if (this.name == null || this.name.Equals(""))
            {
                if (this.fsPath != null && !this.fsPath.Equals(""))
                {
                    FileInfo fi = new FileInfo(this.fsPath);
                    this.name = fi.Name;
                }
            }
        }
    }
}
