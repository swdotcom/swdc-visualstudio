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

        public void ClearTimeDataSummary()
        {
            SaveTimeDataSummaryToDisk(new TimeData());
        }

        public void UpdateTimeSummaryData(long editor_seconds, long session_seconds, long file_seconds)
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            TimeData td = new TimeData();
            td.day = nowTime.local_day;
            td.session_seconds = session_seconds;
            td.editor_seconds = editor_seconds;
            td.file_seconds = file_seconds;
            td.timestamp = nowTime.utc_end_of_day;
            td.timestamp_local = nowTime.local_end_of_day;
            SaveTimeDataSummaryToDisk(td);
        }

        public void SaveTimeDataSummaryToDisk(TimeData timeData)
        {
            string MethodName = "saveTimeDataSummaryToDisk";
            string file = SoftwareCoUtil.getTimeDataFile();

            if (SoftwareCoUtil.TimeDataSummaryFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                File.WriteAllText(file, timeData.GetAsJson(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

        }

        public TimeData GetTimeDataSummary()
        {
            if (!SoftwareCoUtil.TimeDataSummaryFileExists())
            {
                // create it
                SaveTimeDataSummaryToDisk(new TimeData());
            }

            string timeDataSummary = SoftwareCoUtil.getTimeDataFileData();

            try
            {
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(timeDataSummary);
                _timeDataSummary = new TimeData();
                _timeDataSummary = _timeDataSummary.GeTimeSummaryFromDictionary(jsonObj);
            } catch (Exception e)
            {
                _timeDataSummary = new TimeData();
            }
            return _timeDataSummary;
        }

        public async Task SendTimeDataAsync()
        {
            string timeDataSummary = SoftwareCoUtil.getTimeDataFileData();
            if (timeDataSummary != null)
            {
                if (!timeDataSummary.StartsWith("["))
                {
                    // join array around the json string
                    timeDataSummary = "[" + string.Join(",", timeDataSummary) + "]";
                } 
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/time", timeDataSummary);
                if (SoftwareHttpManager.IsOk(response))
                {
                    ClearTimeDataSummary();
                }
            }
        }
    }
}
