using System;
using System.Collections.Generic;

namespace SoftwareCo
{

  public class SessionSummary
  {
    public long currentDayMinutes { get; set; }
    public long averageDailyMinutes { get; set; }


    public JsonObject GetSessionSummaryJson()
    {
      JsonObject jsonObj = new JsonObject();
      jsonObj.Add("currentDayMinutes", this.currentDayMinutes);
      jsonObj.Add("averageDailyMinutes", this.averageDailyMinutes);

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
      dict.Add("averageDailyMinutes", this.averageDailyMinutes);

      return dict;
    }

    public SessionSummary GetSessionSummaryFromDictionary(IDictionary<string, object> dict)
    {
      SessionSummary sessionSummary = new SessionSummary();

      long currDayMinutes = SoftwareCoUtil.GetLongVal(dict, "currentDayMinutes");

      sessionSummary.currentDayMinutes = Math.Max(currDayMinutes, sessionSummary.currentDayMinutes);
      sessionSummary.averageDailyMinutes = SoftwareCoUtil.GetLongVal(dict, "averageDailyMinutes");

      return sessionSummary;
    }

    public void CloneSessionSummary(SessionSummary summary)
    {
      this.currentDayMinutes = summary.currentDayMinutes;
      this.averageDailyMinutes = summary.averageDailyMinutes;
    }

  }
}
