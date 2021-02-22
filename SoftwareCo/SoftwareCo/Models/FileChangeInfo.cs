using System;
using System.Collections.Generic;
using System.IO;

namespace SoftwareCo
{
    public class FileChangeInfo
    {
        public long add { get; set; }
        public long close { get; set; }
        public long delete { get; set; }
        public long linesAdded { get; set; }
        public long linesRemoved { get; set; }
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
            this.fsPath = SoftwareCoUtil.GetStringVal(dict, "fsPath");
            this.name = SoftwareCoUtil.GetStringVal(dict, "name");
            this.add = SoftwareCoUtil.GetLongVal(dict, "add");
            this.close = SoftwareCoUtil.GetLongVal(dict, "close");
            this.delete = SoftwareCoUtil.GetLongVal(dict, "delete");
            this.linesAdded = SoftwareCoUtil.GetLongVal(dict, "linesAdded");
            this.linesRemoved = SoftwareCoUtil.GetLongVal(dict, "linesRemoved");
            this.open = SoftwareCoUtil.GetLongVal(dict, "open");
            this.paste = SoftwareCoUtil.GetLongVal(dict, "paste");
            this.keystrokes = SoftwareCoUtil.GetLongVal(dict, "keystrokes");
            this.netkeys = SoftwareCoUtil.GetLongVal(dict, "netkeys");
            this.syntax = SoftwareCoUtil.GetStringVal(dict, "syntax");
            this.start = SoftwareCoUtil.GetLongVal(dict, "start");
            this.local_start = SoftwareCoUtil.GetLongVal(dict, "local_start");
            this.local_end = SoftwareCoUtil.GetLongVal(dict, "local_end");
            this.duration_seconds = SoftwareCoUtil.GetLongVal(dict, "duration_seconds");
            this.projectDir = SoftwareCoUtil.GetStringVal(dict, "projectDir");
            this.update_count = SoftwareCoUtil.GetLongVal(dict, "update_count");
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
