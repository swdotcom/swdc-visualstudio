using System;
using System.Collections.Generic;

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

        public long averageDailyCodeTimeMinutes { get; set; }
        public long averageDailyMinutes { get; set; }
        public long averageDailyKeystrokes { get; set; }
        public long averageDailyKpm { get; set; }
        public long averageDailyLinesAdded { get; set; }
        public long averageDailyLinesRemoved { get; set; }

        public long globalAverageDailyCodeTimeMinutes { get; set; }
        public long globalAverageSeconds { get; set; }
        public long globalAverageDailyMinutes { get; set; }
        public long globalAverageDailyKeystrokes { get; set; }
        public long globalAverageLinesAdded { get; set; }
        public long globalAverageLinesRemoved { get; set; }


        public JsonObject GetSessionSummaryJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("currentDayMinutes", this.currentDayMinutes);
            jsonObj.Add("currentDayKeystrokes", this.currentDayKeystrokes);
            jsonObj.Add("currentDayKpm", this.currentDayKpm);
            jsonObj.Add("currentDayLinesAdded", this.currentDayLinesAdded);
            jsonObj.Add("currentDayLinesRemoved", this.currentDayLinesRemoved);
            jsonObj.Add("currentSessionGoalPercent", this.currentSessionGoalPercent);

            jsonObj.Add("averageDailyCodeTimeMinutes", this.averageDailyCodeTimeMinutes);
            jsonObj.Add("averageDailyMinutes", this.averageDailyMinutes);
            jsonObj.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            jsonObj.Add("averageDailyKpm", this.averageDailyKpm);
            jsonObj.Add("averageDailyLinesAdded", this.averageDailyLinesAdded);
            jsonObj.Add("averageDailyLinesRemoved", this.averageDailyLinesRemoved);

            jsonObj.Add("globalAverageDailyCodeTimeMinutes", this.globalAverageDailyCodeTimeMinutes);
            jsonObj.Add("globalAverageSeconds", this.globalAverageSeconds);
            jsonObj.Add("globalAverageDailyMinutes", this.globalAverageDailyMinutes);
            jsonObj.Add("globalAverageDailyKeystrokes", this.globalAverageDailyKeystrokes);
            jsonObj.Add("globalAverageLinesAdded", this.globalAverageLinesAdded);
            jsonObj.Add("globalAverageLinesRemoved", this.globalAverageLinesRemoved);

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

            dict.Add("averageDailyCodeTimeMinutes", this.averageDailyCodeTimeMinutes);
            dict.Add("averageDailyMinutes", this.averageDailyMinutes);
            dict.Add("averageDailyKeystrokes", this.averageDailyKeystrokes);
            dict.Add("averageDailyKpm", this.averageDailyKpm);
            dict.Add("averageDailyLinesAdded", this.averageDailyLinesAdded);
            dict.Add("averageDailyLinesRemoved", this.averageDailyLinesRemoved);

            dict.Add("globalAverageDailyCodeTimeMinutes", this.globalAverageDailyCodeTimeMinutes);
            dict.Add("globalAverageSeconds", this.globalAverageSeconds);
            dict.Add("globalAverageDailyMinutes", this.globalAverageDailyMinutes);
            dict.Add("globalAverageDailyKeystrokes", this.globalAverageDailyKeystrokes);
            dict.Add("globalAverageLinesAdded", this.globalAverageLinesAdded);
            dict.Add("globalAverageLinesRemoved", this.globalAverageLinesRemoved);

            return dict;
        }

        public SessionSummary GetSessionSummaryFromDictionary(IDictionary<string, object> dict)
        {
            SessionSummary sessionSummary = new SessionSummary();

            long currDayMinutes = SoftwareCoUtil.GetLongVal(dict, "currentDayMinutes");
            long currDayKeystrokes = SoftwareCoUtil.GetLongVal(dict, "currentDayKeystrokes");
            long currDayLinesAdded = SoftwareCoUtil.GetLongVal(dict, "currentDayLinesAdded");
            long currDayLinesRemoved = SoftwareCoUtil.GetLongVal(dict, "currentDayLinesRemoved");

            sessionSummary.currentDayMinutes = Math.Max(currDayMinutes, sessionSummary.currentDayMinutes);
            sessionSummary.currentDayKeystrokes = Math.Max(currDayKeystrokes, sessionSummary.currentDayKeystrokes);
            sessionSummary.currentDayLinesAdded = Math.Max(currDayLinesAdded, sessionSummary.currentDayLinesAdded);
            sessionSummary.currentDayLinesRemoved = Math.Max(currDayLinesRemoved, sessionSummary.currentDayLinesRemoved);

            sessionSummary.currentDayKpm = SoftwareCoUtil.GetLongVal(dict, "currentDayKpm");
            sessionSummary.averageDailyCodeTimeMinutes = SoftwareCoUtil.GetLongVal(dict, "averageDailyCodeTimeMinutes");
            sessionSummary.averageDailyKeystrokes = SoftwareCoUtil.GetLongVal(dict, "averageDailyKeystrokes");
            sessionSummary.averageDailyKpm = SoftwareCoUtil.GetLongVal(dict, "averageDailyKpm");
            sessionSummary.averageDailyLinesAdded = SoftwareCoUtil.GetLongVal(dict, "averageDailyLinesAdded");
            sessionSummary.averageDailyLinesRemoved = SoftwareCoUtil.GetLongVal(dict, "averageDailyLinesRemoved");
            sessionSummary.averageDailyMinutes = SoftwareCoUtil.GetLongVal(dict, "averageDailyMinutes");
            sessionSummary.globalAverageDailyCodeTimeMinutes = SoftwareCoUtil.GetLongVal(dict, "globalAverageDailyCodeTimeMinutes");
            sessionSummary.globalAverageDailyKeystrokes = SoftwareCoUtil.GetLongVal(dict, "globalAverageDailyKeystrokes");
            sessionSummary.globalAverageDailyMinutes = SoftwareCoUtil.GetLongVal(dict, "globalAverageDailyMinutes");
            sessionSummary.globalAverageLinesAdded = SoftwareCoUtil.GetLongVal(dict, "globalAverageLinesAdded");
            sessionSummary.globalAverageLinesRemoved = SoftwareCoUtil.GetLongVal(dict, "globalAverageLinesRemoved");
            sessionSummary.globalAverageSeconds = SoftwareCoUtil.GetLongVal(dict, "globalAverageSeconds");
            
            return sessionSummary;
        }

        public void CloneSessionSummary(SessionSummary summary)
        {
            this.currentDayMinutes = summary.currentDayMinutes;
            // add the current attributes
            this.currentDayKeystrokes = summary.currentDayKeystrokes;
            this.currentDayKpm = summary.currentDayKpm;
            this.currentDayLinesAdded = summary.currentDayLinesAdded;
            this.currentDayLinesRemoved = summary.currentDayLinesRemoved;

            this.currentSessionGoalPercent = summary.currentSessionGoalPercent;
            this.averageDailyCodeTimeMinutes = summary.averageDailyCodeTimeMinutes;
            this.averageDailyMinutes = summary.averageDailyMinutes;
            this.averageDailyKeystrokes = summary.averageDailyKeystrokes;
            this.averageDailyKpm = summary.averageDailyKpm;
            this.averageDailyLinesAdded = summary.averageDailyLinesAdded;
            this.averageDailyLinesRemoved = summary.averageDailyLinesRemoved;

            this.globalAverageDailyCodeTimeMinutes = summary.globalAverageDailyCodeTimeMinutes;
            this.globalAverageSeconds = summary.globalAverageSeconds;
            this.globalAverageDailyMinutes = summary.globalAverageDailyMinutes;
            this.globalAverageDailyKeystrokes = summary.globalAverageDailyKeystrokes;
            this.globalAverageLinesAdded = summary.globalAverageLinesAdded;
            this.globalAverageLinesRemoved = summary.globalAverageLinesRemoved;
        }

    }
}
