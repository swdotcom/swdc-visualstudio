using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class TimeData
    {
        public long timestamp { get; set; }
        public long timestamp_local { get; set; }
        public long editor_seconds { get; set; }
        public long session_seconds { get; set; }
        public long file_seconds { get; set; }
        public string day { get; set; }

        public TimeData GeTimeSummaryFromDictionary(IDictionary<string, object> dict)
        {
            TimeData summary = new TimeData();

            summary.timestamp = SoftwareCoUtil.ConvertObjectToLong(dict, "timestamp");
            summary.timestamp_local = SoftwareCoUtil.ConvertObjectToLong(dict, "timestamp_local");
            summary.editor_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "editor_seconds");
            summary.session_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "session_seconds");
            summary.file_seconds = SoftwareCoUtil.ConvertObjectToLong(dict, "file_seconds");
            summary.day = SoftwareCoUtil.ConvertObjectToString(dict, "day");

            return summary;
        }

        public string GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("timestamp", this.timestamp);
            jsonObj.Add("timestamp_local", this.timestamp_local);
            jsonObj.Add("editor_seconds", this.editor_seconds);
            jsonObj.Add("session_seconds", this.session_seconds);
            jsonObj.Add("file_seconds", this.file_seconds);
            jsonObj.Add("day", this.day);
            return jsonObj.ToString();
        }
    }
}
