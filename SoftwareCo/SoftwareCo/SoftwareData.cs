using System;
using System.Collections.Generic;

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
        public String data = "0"; // keystroke count

        // start and end are in seconds
        public long start;
        public long end;

        public ProjectInfo project;

        public SoftwareData(ProjectInfo projectInfo)
        {
            long nowMillis = Convert.ToInt64((DateTime.Now - DateTime.MinValue).TotalMilliseconds);
            start = (long) Math.Round((double)(nowMillis / 1000));
            project = projectInfo;
        }

        public void ResetData()
        {
            data = "0";
            source = new JsonObject();
            if (project != null)
            {
                project.ResetData();
            }
            long nowMillis = Convert.ToInt64((DateTime.Now - DateTime.MinValue).TotalMilliseconds);
            start = (long)Math.Round((double)(nowMillis / 1000));
            end = 0L;
        }

        public Boolean HasData()
        {
            long dataCount = 0;
            // these will be the filename keys
            foreach (String key in source.Keys)
            {
                JsonObject fileInfoData = (JsonObject)source[key];
                // go through the properties of this and check if any have data
                // close, open, paste, delete, keys
                foreach (String prop in fileInfoData.Keys)
                {
                    dataCount = Convert.ToInt64(fileInfoData[prop]);
                    if (dataCount > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void UpdateData(String fileName, String property, long dataVal)
        {
            // update the keys count for the file info object
            this.addOrUpdateFileInfo(fileName, property, dataVal);

            // update the overall count
            if (property.Equals("keys"))
            {
                data = Convert.ToString(Convert.ToInt32(data) + dataVal);
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

        public void addOrUpdateFileInfo(String fileName, String property, long count)
        {
            JsonObject fileInfoData = null;
            if (source.ContainsKey(fileName))
            {
                fileInfoData = (JsonObject)source[fileName];
                long dataCount = Convert.ToInt64(fileInfoData[property]) + count;
                
                fileInfoData.Remove(property);
                fileInfoData.Add(property, dataCount);
                source.Remove(fileName);
                source.Add(fileName, fileInfoData);
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
            fileInfoData.Add("length", 0);
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

        public void ResetData()
        {
            // intentially blank for now
        }
    }
}