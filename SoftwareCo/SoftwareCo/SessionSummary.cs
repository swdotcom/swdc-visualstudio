using Commons.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{

     class SessionSummary
     {
        public long currentDayMinutes { get; set; }
        public long averageDailyMinutes { get; set; }
        public long averageDailyKeystrokes { get; set; }
        public long currentDayKeystrokes { get; set; }
        public object liveshareMinutes { get; set; }
        public long latestPayloadTimestamp { get; set; }
        public bool lastUpdatedToday { get; set; }


        public string GetSessionSummaryAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("currentDayMinutes", this.currentDayMinutes);
            jsonObj.Add("averageDailyMinutes", this.averageDailyMinutes);
            jsonObj.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            jsonObj.Add("currentDayKeystrokes", this.currentDayKeystrokes);
            jsonObj.Add("liveshareMinutes", this.liveshareMinutes);
            jsonObj.Add("latestPayloadTimestamp", this.latestPayloadTimestamp);
            jsonObj.Add("lastUpdatedToday", this.lastUpdatedToday);
            return jsonObj.ToString();
        }

      
    }
}
