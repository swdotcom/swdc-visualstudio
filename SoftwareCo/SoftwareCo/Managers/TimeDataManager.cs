﻿
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
            return File.ReadAllText(GetTimeDataFile(), System.Text.Encoding.UTF8);
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
                        string content = new JsonArray().ToString();
                        File.WriteAllText(file, content, Encoding.UTF8);
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

            try
            {
                string content = new JsonArray().ToString();
                content = content.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
                File.WriteAllText(file, content, Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }
        }

        public async Task<TimeData> GetNewTimeDataSummary(PluginDataProject project)
        {
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
            td.editor_seconds = Math.Max(td.editor_seconds, td.session_seconds);
            SaveTimeDataSummaryToDisk(td);
        }

        public async Task UpdateSessionAndFileSeconds(long minutes_since_payload)
        {
            PluginDataProject project = await PluginData.GetPluginProject();

            TimeData td = await GetTodayTimeDataSummary(project);
            long session_seconds = minutes_since_payload * 60;
            td.file_seconds += 60;
            td.session_seconds += session_seconds;
            td.editor_seconds = Math.Max(td.editor_seconds, td.session_seconds);
            td.file_seconds = Math.Min(td.file_seconds, td.session_seconds);

            SaveTimeDataSummaryToDisk(td);
        }

        public void SaveTimeDataSummaryToDisk(TimeData timeData)
        {
            // don't save it to disk if it's null or the project info is null or empty
            if (timeData == null || timeData.project == null ||
                timeData.project.directory == null ||
                timeData.project.directory.Equals(""))
            {
                return;
            }
            string MethodName = "saveTimeDataSummaryToDisk";
            NowTime nowTime = SoftwareCoUtil.GetNowTime();

            List<TimeData> list = GetTimeDataList();
            List<TimeData> listToSave = new List<TimeData>();
            listToSave.Add(timeData);
            string projDir = timeData.project.directory;
            foreach (TimeData td in list)
            {
                string tdDir = td.project != null ? td.project.directory : "";
                if ((!tdDir.Equals(projDir) && !tdDir.Equals("")) ||
                    (tdDir.Equals(projDir) && !td.day.Equals(nowTime.local_day)))
                {
                    listToSave.Add(td);
                }
            }

            JsonArray jsonToSave = BuildJsonObjectFromList(listToSave);

            string file = GetTimeDataFile();
            File.SetAttributes(file, FileAttributes.Normal);

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

        private JsonArray BuildJsonObjectFromList(List<TimeData> tdList)
        {
            JsonArray jsonArr = new JsonArray();

            foreach (TimeData info in tdList)
            {
                jsonArr.Add(info);
            }

            return jsonArr;
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
            return await GetNewTimeDataSummary(proj);
        }

        public List<TimeData> GetTimeDataList()
        {
            List<TimeData> existingList = new List<TimeData>();
            
            string timeDataJson = GetTimeDataFileData();

            JsonArray jsonArrayObj = (JsonArray)SimpleJson.DeserializeObject(timeDataJson);
            foreach (JsonObject jsonObj in jsonArrayObj)
            {
                TimeData td = new TimeData();
                try
                {
                    td.CloneFromDictionary(jsonObj);
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
