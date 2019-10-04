
using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    class SoftwareData
    {

        // TODO: backend driven, we should look at getting a list of types at some point.
        public String type = "Events";
        public bool initialized = false;
        // sublime = 1, vs code = 2, eclipse = 3, intellij = 4, visualstudio = 6, atom = 7
        public int pluginId = Constants.PluginId;
        public String version = "";
        public String os = "";

        // non-hardcoded attributes
        public JsonObject source = new JsonObject();
        public long keystrokes = 0; // keystroke count

        // start and end are in seconds
        public long start;
        public long local_start;
        public long end;
        public long local_end;
        public String timezone;
        public int offset; // in minutes

        public ProjectInfo project;
  
        public SoftwareData(ProjectInfo projectInfo)
        {
            start = SoftwareCoUtil.getNowInSeconds();
            project = projectInfo;
            version = SoftwareCoPackage.GetVersion();
            os = SoftwareCoPackage.GetOs();
        }

        public void ResetData()
        {
            keystrokes = 0;
            source = new JsonObject();
            if (project != null)
            {
                project.ResetData();
            }
            //start = SoftwareCoUtil.getNowInSeconds();
            start = 0L;
            local_start = 0L;
            initialized = false;
        }

        public IDictionary<string, object> GetAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("start", this.start);
            dict.Add("local_start", this.local_start);
            dict.Add("pluginId", this.pluginId);
            dict.Add("keystrokes", this.keystrokes);
            dict.Add("type", this.type);
            dict.Add("project", this.project.GetAsDictionary());
            dict.Add("source", this.GetSourceDictionary());
            dict.Add("timezone", this.timezone);
            dict.Add("offset", this.offset);
            return dict;
        }

        public string GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("start", this.start);
            jsonObj.Add("local_start", this.local_start);
            jsonObj.Add("pluginId", this.pluginId);
            jsonObj.Add("type", this.type);
            jsonObj.Add("keystrokes", this.keystrokes);
            jsonObj.Add("source", this.source);
            jsonObj.Add("project", this.project.GetAsJson());
            jsonObj.Add("timezone", this.timezone);
            jsonObj.Add("offset", this.offset);
            jsonObj.Add("version", this.version);
            jsonObj.Add("os", this.os);
            jsonObj.Add("end", this.end);
            jsonObj.Add("local_end", this.local_end);
            return jsonObj.ToString();
        }


        private IDictionary<string, object> GetSourceDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            foreach (String key in source.Keys)
            {
                IDictionary<string, object> innerDict = new Dictionary<string, object>();
                JsonObject fileInfoData = (JsonObject)source[key];
                // go through the properties of this and check if any have data
                // close, open, paste, delete, keys
                foreach (String prop in fileInfoData.Keys)
                {
                    innerDict.Add(prop, fileInfoData[prop]);
                }
                dict.Add(key, innerDict);
            }
            return dict;
        }

        public Boolean HasData()
        {

            if (this.initialized && (this.keystrokes > 0 || this.source.Count>0) && this.project != null && this.project.name != null) {
                return true;
            }
            return false;
        }

        public void UpdateData(String fileName, String property, long dataVal)
        {
            // update the keys count for the file info object
            this.addOrUpdateFileInfo(fileName, property, dataVal);

            // update the overall count
            if (property.Equals("add") || property.Equals("delete"))
            {

                // update the "netkeys"
                if (property.Equals("add"))
                {
                    // "add"
                    // add this to the "netkeys"
                    this.addOrUpdateFileInfo(fileName, "netkeys", dataVal);
                }
                else
                {
                    // "delete"
                    // subtract from the "netkeys"
                    this.addOrUpdateFileInfo(fileName, "netkeys", dataVal/-1);
                }
            }
        }

        public long getFileInfoDataForProperty(String fileName, String property)
        {
            if (source.ContainsKey(fileName))
            {
                JsonObject fileInfoData = (JsonObject)source[fileName];
                return Convert.ToInt64(fileInfoData[property]);
            }

            return 0;
        }

        public void addOrUpdateFileStringInfo(String fileName, String property, string value)
        {
            JsonObject fileInfoData = null;
            if (source.ContainsKey(fileName))
            {
                fileInfoData = (JsonObject)source[fileName];

                fileInfoData.Remove(property);
                fileInfoData.Add(property, value);
                return;
            }
        }

        public void addOrUpdateFileInfo(String fileName, String property, long count)
        {
            JsonObject fileInfoData = null;
            if (source.ContainsKey(fileName))
            {
                fileInfoData = (JsonObject)source[fileName];
                // sum up the previous amount with the count coming in

                long dataCount = 0;
                if (property.Equals("length") || property.Equals("lines"))
                {
                    dataCount = count;
                }
                if (property.Equals("end") || property.Equals("local_end"))
                {
                    dataCount = count;
                }
                else
                {
                    dataCount = Convert.ToInt64(fileInfoData[property]) + count;
                }
                
                fileInfoData.Remove(property);
                fileInfoData.Add(property, dataCount);
                return;
            }
        }

        public void EnsureFileInfoDataIsPresent(string fileName,NowTime nowTime)
        {
            JsonObject fileInfoData     = new JsonObject();
            //long start                  = SoftwareCoUtil.getNowInSeconds();
            //double offset               = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            //long local_start            = start + ((int)offset * 60);\
            long start        = nowTime.now;
            long local_start  = nowTime.local_now;
            if (!source.ContainsKey(fileName))
            {
                fileInfoData.Add("paste", 0);
                fileInfoData.Add("open", 0);
                fileInfoData.Add("close", 0);
                fileInfoData.Add("delete", 0);
                fileInfoData.Add("add", 0);
                fileInfoData.Add("netkeys", 0);
                fileInfoData.Add("length", 0);
                fileInfoData.Add("lines", 0);
                fileInfoData.Add("linesAdded", 0);
                fileInfoData.Add("linesRemoved", 0);
                fileInfoData.Add("syntax", "");
                fileInfoData.Add("start", start);
                fileInfoData.Add("local_start", local_start);
                fileInfoData.Add("end", 0);
                fileInfoData.Add("local_end", 0);
                source.Add(fileName, fileInfoData);

            }
        }
        
        private JsonObject getFileInfoFromSource(String sourceVal)
        {
            if (source == null || !source.ContainsKey(sourceVal))
            {
                return null;
            }
            return (JsonObject)source[sourceVal];
        }
    }

    class ProjectInfo
    {
        public String name;
        public String directory;

        public ProjectInfo(String nameVal, String directoryVal)
        {
            name = nameVal;
            directory = directoryVal;
        }

        public IDictionary<string, object> GetAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("name", this.name);
            dict.Add("directory", this.directory);
            return dict;
        }

        public JsonObject GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("name", this.name);
            jsonObj.Add("directory", this.directory);
            return jsonObj;
        }

        public void ResetData()
        {
            // intentially blank for now
        }
    }
}
 
 