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
        public long currentDayMinutes       = 0;
        public long averageDailyMinutes     = 0;
        public long averageDailyKeystrokes  = 0;
        public long currentDayKeystrokes    = 0;
        public long liveshareMinutes        = 0;
        

        //public SessionSummary()
        //{
        //    currentDayMinutes       = 0;
        //    averageDailyMinutes     = 0;
        //    averageDailyKeystrokes  = 0;
        //    currentDayKeystrokes    = 0;
        //    liveshareMinutes        = 0;
        //}

     

        public string GetSessionSummaryAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("currentDayMinutes", this.currentDayMinutes);
            jsonObj.Add("averageDailyMinutes", this.averageDailyMinutes);
            jsonObj.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            jsonObj.Add("currentDayKeystrokes", this.currentDayKeystrokes);
            jsonObj.Add("liveshareMinutes", this.liveshareMinutes);
            return jsonObj.ToString();
        }

      



    }
}
