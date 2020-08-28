using System.Collections.Generic;

namespace SoftwareCo
{
    public class PluginDataFileInfo
    {
        public string file { get; set; }
        public int add { get; set; }
        public int close { get; set; }
        public int delete { get; set; }
        public int lines { get; set; }
        public int charsPasted { get; set; }
        public int linesAdded { get; set; }
        public int linesRemoved { get; set; }
        public int open { get; set; }
        public int paste { get; set; }
        public int keystrokes { get; set; }
        public int netkeys { get; set; }
        public string syntax { get; set; }
        public long length { get; set; }
        public long start { get; set; }
        public long local_start { get; set; }
        public long end { get; set; }
        public long local_end { get; set; }

        public int characters_added { get; set; }
        public int characters_deleted { get; set; }
        public int single_deletes { get; set; }
        public int multi_deletes { get; set; }
        public int single_adds { get; set; }
        public int multi_adds { get; set; }
        public int auto_indents { get; set; }
        public int replacements { get; set; }
        public bool is_net_change { get; set; }

        public PluginDataFileInfo(string file)
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            this.file = file;
            this.start = nowTime.now;
            this.local_start = nowTime.local_now;
            this.syntax = "";
            this.end = this.start + 60;
            this.local_end = this.local_start + 60;
            this.is_net_change = false;
        }

        public JsonObject GetPluginDataFileInfoAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("file", this.file);
            jsonObj.Add("length", this.length);
            jsonObj.Add("add", this.add);
            jsonObj.Add("close", this.close);
            jsonObj.Add("delete", this.delete);
            jsonObj.Add("charsPasted", this.charsPasted);
            jsonObj.Add("linesAdded", this.linesAdded);
            jsonObj.Add("linesRemoved", this.linesRemoved);
            jsonObj.Add("lines", this.lines);
            jsonObj.Add("open", this.open);
            jsonObj.Add("paste", this.paste);
            jsonObj.Add("keystrokes", this.keystrokes);
            jsonObj.Add("netkeys", this.netkeys);
            jsonObj.Add("syntax", this.syntax);
            jsonObj.Add("start", this.start);
            jsonObj.Add("end", this.end);
            jsonObj.Add("local_start", this.local_start);
            jsonObj.Add("characters_added", this.characters_added);
            jsonObj.Add("characters_deleted", this.characters_deleted);
            jsonObj.Add("auto_indents", this.auto_indents);
            jsonObj.Add("single_adds", this.single_adds);
            jsonObj.Add("multi_adds", this.multi_adds);
            jsonObj.Add("single_deletes", this.single_deletes);
            jsonObj.Add("multi_deletes", this.multi_deletes);
            jsonObj.Add("replacements", this.replacements);
            jsonObj.Add("is_net_change", this.is_net_change);
            return jsonObj;
        }

        public static PluginDataFileInfo GetPluginDataFromDict(IDictionary<string, object> dict)
        {
            string file = SoftwareCoUtil.ConvertObjectToString(dict, "file");
            PluginDataFileInfo fileInfo = new PluginDataFileInfo(file);
            fileInfo.start = SoftwareCoUtil.ConvertObjectToLong(dict, "start");
            fileInfo.length = SoftwareCoUtil.ConvertObjectToLong(dict, "length");
            fileInfo.local_start = SoftwareCoUtil.ConvertObjectToLong(dict, "local_start");
            fileInfo.end = SoftwareCoUtil.ConvertObjectToLong(dict, "end");
            fileInfo.local_end = SoftwareCoUtil.ConvertObjectToLong(dict, "local_end");
            fileInfo.lines = SoftwareCoUtil.ConvertObjectToInt(dict, "lines");
            fileInfo.charsPasted = SoftwareCoUtil.ConvertObjectToInt(dict, "charsPasted");
            fileInfo.add = SoftwareCoUtil.ConvertObjectToInt(dict, "add");
            fileInfo.close = SoftwareCoUtil.ConvertObjectToInt(dict, "close");
            fileInfo.paste = SoftwareCoUtil.ConvertObjectToInt(dict, "paste");
            fileInfo.netkeys = SoftwareCoUtil.ConvertObjectToInt(dict, "netkeys");
            fileInfo.linesAdded = SoftwareCoUtil.ConvertObjectToInt(dict, "linesAdded");
            fileInfo.linesRemoved = SoftwareCoUtil.ConvertObjectToInt(dict, "linesRemoved");
            fileInfo.delete = SoftwareCoUtil.ConvertObjectToInt(dict, "delete");
            fileInfo.syntax = SoftwareCoUtil.ConvertObjectToString(dict, "syntax");

            fileInfo.characters_added = SoftwareCoUtil.ConvertObjectToInt(dict, "characters_added");
            fileInfo.characters_deleted = SoftwareCoUtil.ConvertObjectToInt(dict, "characters_deleted");
            fileInfo.auto_indents = SoftwareCoUtil.ConvertObjectToInt(dict, "auto_indents");
            fileInfo.single_adds = SoftwareCoUtil.ConvertObjectToInt(dict, "single_adds");
            fileInfo.multi_adds = SoftwareCoUtil.ConvertObjectToInt(dict, "multi_adds");
            fileInfo.single_deletes = SoftwareCoUtil.ConvertObjectToInt(dict, "single_deletes");
            fileInfo.multi_deletes = SoftwareCoUtil.ConvertObjectToInt(dict, "multi_deletes");
            fileInfo.replacements = SoftwareCoUtil.ConvertObjectToInt(dict, "replacements");
            fileInfo.is_net_change = SoftwareCoUtil.ConvertObjectToBool(dict, "is_net_change");

            return fileInfo;
        }

        public IDictionary<string, object> GetPluginDataFileInfoAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("file", this.file);
            dict.Add("length", this.length);
            dict.Add("add", this.add);
            dict.Add("close", this.close);
            dict.Add("delete", this.delete);
            dict.Add("charsPasted", this.charsPasted);
            dict.Add("lines", this.lines);
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

            dict.Add("characters_added", this.characters_added);
            dict.Add("characters_deleted", this.characters_deleted);
            dict.Add("auto_indents", this.auto_indents);
            dict.Add("single_adds", this.single_adds);
            dict.Add("multi_adds", this.multi_adds);
            dict.Add("single_deletes", this.single_deletes);
            dict.Add("multi_deletes", this.multi_deletes);
            dict.Add("replacements", this.replacements);
            dict.Add("is_net_change", this.is_net_change);

            return dict;
        }

        public void EndFileInfoTime(NowTime nowTime)
        {
            this.end = nowTime.now;
            this.local_end = nowTime.local_now;
        }
    }
}
