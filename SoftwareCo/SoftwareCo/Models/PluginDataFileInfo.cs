using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class PluginDataFileInfo
    {
        public string file { get; set; }
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
        public long length { get; set; }
        public long start { get; set; }
        public long local_start { get; set; }
        public long end { get; set; }
        public long local_end { get; set; }
        public long cumulative_editor_seconds { get; set; }

        public PluginDataFileInfo(string file)
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            this.file = file;
            this.start = nowTime.now;
            this.local_start = nowTime.local_now;
            this.add = 0;
            this.close = 0;
            this.linesAdded = 0;
            this.open = 0;
            this.keystrokes = 0;
            this.netkeys = 0;
            this.syntax = "";
            this.length = 0;
            this.end = this.start + 60;
            this.local_end = this.local_start + 60;
            this.cumulative_editor_seconds = 0;
        }

        public JsonObject GetPluginDataFileInfoAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("file", this.file);
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
            jsonObj.Add("cumulative_editor_seconds", this.cumulative_editor_seconds);
            return jsonObj;
        }

        public IDictionary<string, object> GetPluginDataFileInfoAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("file", this.file);
            dict.Add("add", this.add);
            dict.Add("close", this.close);
            dict.Add("delete", this.delete);
            dict.Add("linesAdded", this.linesAdded);
            dict.Add("linesRemoved", this.linesRemoved);
            dict.Add("open", this.open);
            dict.Add("paste", this.paste);
            dict.Add("keystrokes", this.keystrokes);
            dict.Add("netkeys", this.netkeys);
            dict.Add("syntax", this.syntax);
            dict.Add("start", this.start);
            dict.Add("end", this.end);
            dict.Add("local_start", this.local_start);
            dict.Add("cumulative_editor_seconds", this.cumulative_editor_seconds);
            return dict;
        }

        public void EndFileInfoTime(NowTime nowTime)
        {
            this.end = nowTime.now;
            this.local_end = nowTime.local_now;
        }
    }
}
