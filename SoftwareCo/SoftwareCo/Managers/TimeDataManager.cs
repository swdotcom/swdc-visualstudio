
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class TimeDataManager
    {
        private static readonly Lazy<TimeDataManager> lazy = new Lazy<TimeDataManager>(() => new TimeDataManager());

        private TimeData _timeDataSummary;

        public static TimeDataManager Instance { get { return lazy.Value; } }

        private TimeDataManager()
        {
            //
        }

        public static String GetTimeDataFileData()
        {
            // make sure it's created
            GetTimeDataFile();
            return File.ReadAllText(SoftwareCoUtil.getSoftwareDataDir(true) + "\\projectTimeData.json", System.Text.Encoding.UTF8);
        }

        public static String GetTimeDataFile()
        {
            try
            {
                string file = SoftwareCoUtil.getSoftwareDataDir(true) + "\\projectTimeData.json";
                if (!File.Exists(file))
                {
                    try
                    {
                        string content = "[]";
                        File.WriteAllText(file, content, System.Text.Encoding.UTF8);
                    }
                    catch (Exception e)
                    {
                        //
                    }
                }
                return file;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public void ClearTimeDataSummary()
        {
            string file = GetTimeDataFile();
            List<TimeData> list = new List<TimeData>();
            JsonObject jsonToSave = BuildJsonObjectFromList(list);

            try
            {
                string content = jsonToSave.ToString();
                content = content.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
                File.WriteAllText(file, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }
        }

        public async Task<TimeData> GetNewTimeDataSummary()
        {
            PluginDataProject project = await PluginData.GetPluginProject();
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            TimeData td = new TimeData();
            td.day = nowTime.local_day;
            td.timestamp = nowTime.utc_end_of_day;
            td.timestamp_local = nowTime.local_end_of_day;
            td.project = project;

            return td;
        }

        public async Task UpdateEditorSeconds(long editor_seconds)
        {
            PluginDataProject project = await PluginData.GetPluginProject();

            TimeData td = await GetTodayTimeDataSummary(project);
            td.editor_seconds += editor_seconds;
            SaveTimeDataSummaryToDisk(td);
        }

        public async Task UpdateSessionAndFileSeconds(long minutes_since_payload)
        {
            PluginDataProject project = await PluginData.GetPluginProject();

            TimeData td = await GetTodayTimeDataSummary(project);
            long session_seconds = minutes_since_payload * 60;
            td.file_seconds += 60;
            td.session_seconds += session_seconds;

            SaveTimeDataSummaryToDisk(td);
        }

        public void SaveTimeDataSummaryToDisk(TimeData timeData)
        {
            string MethodName = "saveTimeDataSummaryToDisk";
            string file = GetTimeDataFile();
            NowTime nowTime = SoftwareCoUtil.GetNowTime();

            File.SetAttributes(file, FileAttributes.Normal);

            List<TimeData> list = GetTimeDataList();
            string projDir = timeData.project != null ? timeData.project.directory : "";
            bool foundIt = false;
            foreach (TimeData td in list)
            {
                string tdDir = td.project != null ? td.project.directory : "";
                if (td.day.Equals(nowTime.local_day) && tdDir.Equals(projDir))
                {
                    td.Clone(timeData);
                    foundIt = true;
                    break;
                }
            }

            if (!foundIt)
            {
                list.Add(timeData);
            }

            JsonObject jsonToSave = BuildJsonObjectFromList(list);

            try
            {
                string content = jsonToSave.ToString();
                content = content.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
                File.WriteAllText(file, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

        }

        private JsonObject BuildJsonObjectFromList(List<TimeData> tdList)
        {
            JsonObject jsonObj = new JsonObject();

            foreach (TimeData info in tdList)
            {
                string key = info.day + "_" + info.project.directory;
                jsonObj.Add(key, info.GetAsJson());
            }

            return jsonObj;
        }

        public async Task<TimeData> GetTodayTimeDataSummary(PluginDataProject proj)
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            List<TimeData> list = GetTimeDataList();
            string projDir = proj != null ? proj.directory : "";
            if (list != null && list.Count > 0)
            {
                foreach (TimeData td in list)
                {
                    string tdDir = td.project != null ? td.project.directory : "";
                    if (td.day.Equals(nowTime.local_day) && tdDir.Equals(projDir))
                    {
                        return td;
                    }
                }
            }
            return await GetNewTimeDataSummary();
        }

        public List<TimeData> GetTimeDataList()
        {
            List<TimeData> existingList = new List<TimeData>();
            
            string timeDataJson = GetTimeDataFileData();

            IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(timeDataJson);
            foreach (string key in jsonObj.Keys)
            {
                TimeData td = new TimeData();

                jsonObj.TryGetValue(key, out object infoObj);
                try
                {
                    JsonObject infoObjJson = (infoObj == null) ? null : (JsonObject)infoObj;
                    if (infoObjJson != null)
                    {
                        td.CloneFromDictionary(infoObjJson);
                    }
                }
                catch (Exception e)
                {
                    //
                }

                existingList.Add(td);
            }
            return existingList;
        }


        public async Task SendTimeDataAsync()
        {
            string timeDataSummary = GetTimeDataFileData();
            if (timeDataSummary != null)
            {
                if (!timeDataSummary.StartsWith("["))
                {
                    // join array around the json string
                    timeDataSummary = "[" + string.Join(",", timeDataSummary) + "]";
                } 
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                    HttpMethod.Post, "/data/time", timeDataSummary);
                if (SoftwareHttpManager.IsOk(response))
                {
                    ClearTimeDataSummary();
                }
            }
        }
    }
}
