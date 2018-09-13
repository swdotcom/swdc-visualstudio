using System;
using System.Collections.Generic;
using System.Text;
using Commons.Json;

namespace SoftwareCo
{
    class SoftwareData
    {

        // TODO: backend driven, we should look at getting a list of types at some point
        public String type = "Events";
        // sublime = 1, vs code = 2, eclipse = 3, intellij = 4, visualstudio = 6, atom = 7
        public int pluginId = 6;

        // non-hardcoded attributes
        public JsonObject source = new JsonObject();
        public long data = 0; // keystroke count

        // start and end are in seconds
        public long start;
        public long end;

        public ProjectInfo project;

        public SoftwareData(ProjectInfo projectInfo)
        {
            start = SoftwareCoPackage.getNowInSeconds();
            project = projectInfo;
        }

        public void ResetData()
        {
            data = 0;
            source = new JsonObject();
            if (project != null)
            {
                project.ResetData();
            }
            start = SoftwareCoPackage.getNowInSeconds();
            end = 0L;
        }

        public IDictionary<string, object> GetAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("start", this.start);
            dict.Add("end", this.end);
            dict.Add("pluginId", this.pluginId);
            dict.Add("data", this.data);
            dict.Add("type", this.type);
            dict.Add("project", this.project.GetAsDictionary());
            dict.Add("source", this.GetSourceDictionary());
            return dict;
        }

        public string GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("start", this.start);
            jsonObj.Add("end", this.end);
            jsonObj.Add("pluginId", this.pluginId);
            jsonObj.Add("type", this.type);
            jsonObj.Add("data", this.data);
            jsonObj.Add("source", this.source);
            jsonObj.Add("project", this.project.GetAsJson());
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
            long dataCount = 0;
            if (this.data > 0) {
                return true;
            }
            // these will be the filename keys
            foreach (String key in source.Keys)
            {
                JsonObject fileInfoData = (JsonObject)source[key];
                // go through the properties of this and check if any have data
                // close, open, paste, delete, keys
                foreach (String prop in fileInfoData.Keys)
                {
                    try
                    {
                        dataCount = Convert.ToInt64(fileInfoData[prop]);
                        if (dataCount > 0)
                        {
                            return true;
                        }
                    }
                    catch (Exception e)
                    {
                        // not a int 64 value
                    }
                }
            }
            return false;
        }

        public void UpdateData(String fileName, String property, long dataVal)
        {
            // update the keys count for the file info object
            this.addOrUpdateFileInfo(fileName, property, dataVal);

            this.updateDataCount(property);

            // update the overall count
            if (property.Equals("add") || property.Equals("delete"))
            {

                // update the "keys" and "netkeys"
                if (property.Equals("add"))
                {
                    // "add"
                    // add this to the "keys"and "netkeys"
                    this.addOrUpdateFileInfo(fileName, "keys", dataVal);
                    this.addOrUpdateFileInfo(fileName, "netkeys", dataVal);
                }
                else
                {
                    // "delete"
                    // add this to the "keys" and subtract from the "netkeys"
                    this.addOrUpdateFileInfo(fileName, "keys", dataVal);
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

        private void updateDataCount(String property)
        {
            if (property.Equals("add") || property.Equals("delete")
                || property.Equals("paste") || property.Equals("linesAdded")
                || property.Equals("linesRemoved"))
            {
                data += 1;
            }
        }

        public void addOrUpdateFileInfo(String fileName, String property, long count)
        {

            this.updateDataCount(property);

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
                else
                {
                    dataCount = Convert.ToInt64(fileInfoData[property]) + count;
                }
                
                
                fileInfoData.Remove(property);
                fileInfoData.Add(property, dataCount);
                return;
            }

            //
            // not found, add it

            fileInfoData = new JsonObject();
            fileInfoData.Add("paste", 0);
            fileInfoData.Add("open", 0);
            fileInfoData.Add("close", 0);
            fileInfoData.Add("delete", 0);
            fileInfoData.Add("keys", 0);
            fileInfoData.Add("add", 0);
            fileInfoData.Add("netkeys", 0);
            fileInfoData.Add("length", 0);
            fileInfoData.Add("lines", 0);
            fileInfoData.Add("linesAdded", 0);
            fileInfoData.Add("linesRemoved", 0);
            fileInfoData.Add("syntax", "");
            if (property != null && count > 0)
            {
                fileInfoData.Remove(property);
                fileInfoData.Add(property, count);
            }
            source.Add(fileName, fileInfoData);
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