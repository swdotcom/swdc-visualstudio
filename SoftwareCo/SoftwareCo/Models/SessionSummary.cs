using Commons.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{

     public class SessionSummary
     {
        public long currentDayMinutes { get; set; }
        public long currentDayKeystrokes { get; set; }
        public long currentDayKpm { get; set; }
        public long currentDayLinesAdded { get; set; }
        public long currentDayLinesRemoved { get; set; }
        public double currentSessionGoalPercent { get; set; }

        public long averageDailyMinutes { get; set; }
        public long averageDailyKeystrokes { get; set; }
        public long averageDailyKpm { get; set; }
        public long averageDailyLinesAdded { get; set; }
        public long averageDailyLinesRemoved { get; set; }

        public long globalAverageSeconds { get; set; }
        public long globalAverageDailyMinutes { get; set; }
        public long globalAverageDailyKeystrokes { get; set; }
        public long globalAverageLinesAdded { get; set; }
        public long globalAverageLinesRemoved { get; set; }

        public bool inflow { get; set; }
        public double timePercent { get; set; }
        public double volumePercent { get; set; }
        public double velocityPercent { get; set; }

        public int liveshareMinutes { get; set; }
        public long latestPayloadTimestamp { get; set; }
        public long latestPayloadTimestampEndUtc { get; set; }
        public bool lastUpdatedToday { get; set; }

        public int dailyMinutesGoal { get; set; }


        public JsonObject GetSessionSummaryJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("currentDayMinutes", this.currentDayMinutes);
            jsonObj.Add("currentDayKeystrokes", this.currentDayKeystrokes);
            jsonObj.Add("currentDayKpm", this.currentDayKpm);
            jsonObj.Add("currentDayLinesAdded", this.currentDayLinesAdded);
            jsonObj.Add("currentDayLinesRemoved", this.currentDayLinesRemoved);
            jsonObj.Add("currentSessionGoalPercent", this.currentSessionGoalPercent);

            jsonObj.Add("averageDailyMinutes", this.averageDailyMinutes);
            jsonObj.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            jsonObj.Add("averageDailyKpm", this.averageDailyKpm);
            jsonObj.Add("averageDailyLinesAdded", this.averageDailyLinesAdded);
            jsonObj.Add("averageDailyLinesRemoved", this.averageDailyLinesRemoved);

            jsonObj.Add("globalAverageSeconds", this.globalAverageSeconds);
            jsonObj.Add("globalAverageDailyMinutes", this.globalAverageDailyMinutes);
            jsonObj.Add("globalAverageDailyKeystrokes", this.globalAverageDailyKeystrokes);
            jsonObj.Add("globalAverageLinesAdded", this.globalAverageLinesAdded);
            jsonObj.Add("globalAverageLinesRemoved", this.globalAverageLinesRemoved);

            jsonObj.Add("inflow", this.inflow);
            jsonObj.Add("timePercent", this.timePercent);
            jsonObj.Add("volumePercent", this.volumePercent);
            jsonObj.Add("velocityPercent", this.velocityPercent);

            jsonObj.Add("liveshareMinutes", this.liveshareMinutes);
            jsonObj.Add("latestPayloadTimestamp", this.latestPayloadTimestamp);
            jsonObj.Add("latestPayloadTimestampEndUtc", this.latestPayloadTimestampEndUtc);
            jsonObj.Add("lastUpdatedToday", this.lastUpdatedToday);

            jsonObj.Add("dailyMinutesGoal", this.dailyMinutesGoal);
            return jsonObj;
        }

        public string GetSessionSummaryJsonString()
        {
            return GetSessionSummaryJson().ToString();
        }

        public IDictionary<string, object> GetSessionSummaryDict()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("currentDayMinutes", this.currentDayMinutes);
            dict.Add("currentDayKeystrokes", this.currentDayKeystrokes);
            dict.Add("currentDayKpm", this.currentDayKpm);
            dict.Add("currentDayLinesAdded", this.currentDayLinesAdded);
            dict.Add("currentDayLinesRemoved", this.currentDayLinesRemoved);
            dict.Add("currentSessionGoalPercent", this.currentSessionGoalPercent);

            dict.Add("averageDailyMinutes", this.averageDailyMinutes);
            dict.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            dict.Add("averageDailyKpm", this.averageDailyKpm);
            dict.Add("averageDailyLinesAdded", this.averageDailyLinesAdded);
            dict.Add("averageDailyLinesRemoved", this.averageDailyLinesRemoved);

            dict.Add("globalAverageSeconds", this.globalAverageSeconds);
            dict.Add("globalAverageDailyMinutes", this.globalAverageDailyMinutes);
            dict.Add("globalAverageDailyKeystrokes", this.globalAverageDailyKeystrokes);
            dict.Add("globalAverageLinesAdded", this.globalAverageLinesAdded);
            dict.Add("globalAverageLinesRemoved", this.globalAverageLinesRemoved);

            dict.Add("inflow", this.inflow);
            dict.Add("timePercent", this.timePercent);
            dict.Add("volumePercent", this.volumePercent);
            dict.Add("velocityPercent", this.velocityPercent);

            dict.Add("liveshareMinutes", this.liveshareMinutes);
            dict.Add("latestPayloadTimestamp", this.latestPayloadTimestamp);
            dict.Add("latestPayloadTimestampEndUtc", this.latestPayloadTimestampEndUtc);
            dict.Add("lastUpdatedToday", this.lastUpdatedToday);

            dict.Add("dailyMinutesGoal", this.dailyMinutesGoal);
            return dict;
        }

        public SessionSummary GetSessionSummaryFromDictionary(IDictionary<string, object> dict)
        {
            SessionSummary sessionSummary = new SessionSummary();

            sessionSummary.currentDayMinutes = SoftwareCoUtil.ConvertObjectToLong(dict, "currentDayMinutes");
            sessionSummary.currentDayKeystrokes = SoftwareCoUtil.ConvertObjectToLong(dict, "currentDayKeystrokes");
            sessionSummary.currentDayKpm = SoftwareCoUtil.ConvertObjectToLong(dict, "currentDayKpm");
            sessionSummary.currentDayLinesAdded = SoftwareCoUtil.ConvertObjectToLong(dict, "currentDayLinesAdded");
            sessionSummary.currentDayLinesRemoved = SoftwareCoUtil.ConvertObjectToLong(dict, "currentDayLinesRemoved");
            sessionSummary.averageDailyKeystrokes = SoftwareCoUtil.ConvertObjectToLong(dict, "averageDailyKeystrokes");
            sessionSummary.averageDailyKpm = SoftwareCoUtil.ConvertObjectToLong(dict, "averageDailyKpm");
            sessionSummary.averageDailyLinesAdded = SoftwareCoUtil.ConvertObjectToLong(dict, "averageDailyLinesAdded");
            sessionSummary.averageDailyLinesRemoved = SoftwareCoUtil.ConvertObjectToLong(dict, "averageDailyLinesRemoved");
            sessionSummary.averageDailyMinutes = SoftwareCoUtil.ConvertObjectToLong(dict, "averageDailyMinutes");
            sessionSummary.globalAverageDailyKeystrokes = SoftwareCoUtil.ConvertObjectToLong(dict, "globalAverageDailyKeystrokes");
            sessionSummary.globalAverageDailyMinutes = SoftwareCoUtil.ConvertObjectToLong(dict, "globalAverageDailyMinutes");
            sessionSummary.globalAverageLinesAdded = SoftwareCoUtil.ConvertObjectToLong(dict, "globalAverageLinesAdded");
            sessionSummary.globalAverageLinesRemoved = SoftwareCoUtil.ConvertObjectToLong(dict, "globalAverageLinesRemoved");
            sessionSummary.globalAverageSeconds = SoftwareCoUtil.ConvertObjectToLong(dict, "globalAverageSeconds");

            sessionSummary.latestPayloadTimestamp = SoftwareCoUtil.ConvertObjectToLong(dict, "latestPayloadTimestamp");
            sessionSummary.latestPayloadTimestampEndUtc = SoftwareCoUtil.ConvertObjectToLong(dict, "latestPayloadTimestampEndUtc");
            sessionSummary.timePercent = SoftwareCoUtil.ConvertObjectToDouble(dict, "timePercent");
            sessionSummary.velocityPercent = SoftwareCoUtil.ConvertObjectToDouble(dict, "velocityPercent");
            sessionSummary.volumePercent = SoftwareCoUtil.ConvertObjectToDouble(dict, "volumePercent");
            sessionSummary.dailyMinutesGoal = SoftwareCoUtil.ConvertObjectToInt(dict, "dailyMinutesGoal");
            sessionSummary.liveshareMinutes = SoftwareCoUtil.ConvertObjectToInt(dict, "liveshareMinutes");

            sessionSummary.inflow = SoftwareCoUtil.ConvertObjectToBool(dict, "inflow");
            sessionSummary.lastUpdatedToday = SoftwareCoUtil.ConvertObjectToBool(dict, "lastUpdatedToday");

            return sessionSummary;
        }

        public void CloneSessionSummary(SessionSummary summary)
        {
            if (this.currentDayMinutes < summary.currentDayMinutes)
            {
                // add the current attributes
                this.currentDayMinutes = summary.currentDayMinutes;
                this.currentDayKeystrokes = summary.currentDayKeystrokes;
                this.currentDayKpm = summary.currentDayKpm;
                this.currentDayLinesAdded = summary.currentDayLinesAdded;
                this.currentDayLinesRemoved = summary.currentDayLinesRemoved;
            }

            this.currentSessionGoalPercent = summary.currentSessionGoalPercent;
            this.averageDailyMinutes = summary.averageDailyMinutes;
            this.averageDailyKeystrokes = summary.averageDailyKeystrokes;
            this.averageDailyKpm = summary.averageDailyKpm;
            this.averageDailyLinesAdded = summary.averageDailyLinesAdded;
            this.averageDailyLinesRemoved = summary.averageDailyLinesRemoved;

            this.globalAverageSeconds = summary.globalAverageSeconds;
            this.globalAverageDailyMinutes = summary.globalAverageDailyMinutes;
            this.globalAverageDailyKeystrokes = summary.globalAverageDailyKeystrokes;
            this.globalAverageLinesAdded = summary.globalAverageLinesAdded;
            this.globalAverageLinesRemoved = summary.globalAverageLinesRemoved;

            this.inflow = summary.inflow;
            this.timePercent = summary.timePercent;
            this.volumePercent = summary.volumePercent;
            this.velocityPercent = summary.velocityPercent;

            this.liveshareMinutes = summary.liveshareMinutes;
            this.latestPayloadTimestamp = summary.latestPayloadTimestamp;
            this.latestPayloadTimestampEndUtc = summary.latestPayloadTimestampEndUtc;
            this.lastUpdatedToday = summary.lastUpdatedToday;

            this.dailyMinutesGoal = summary.dailyMinutesGoal;
        }
      
    }
}
