using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;

namespace SoftwareCo
{
    public sealed class SessionSummaryManager
    {
        private static readonly Lazy<SessionSummaryManager> lazy = new Lazy<SessionSummaryManager>(() => new SessionSummaryManager());

        private SessionSummary _sessionSummary;

        public static SessionSummaryManager Instance { get { return lazy.Value; } }

        private SessionSummaryManager()
        {
            // initialize the session summary
            GetSessionSummayData();
        }

        public void IncrementSessionSummaryData(KeystrokeAggregates aggregate)
        {
            WallclockManager wcMgr = WallclockManager.Instance;
            _sessionSummary = GetSessionSummayData();

            long incrementMinutes = GetMinutesSinceLastPayload();
            _sessionSummary.currentDayMinutes += incrementMinutes;

            wcMgr.UpdateBasedOnSessionSeconds(_sessionSummary.currentDayMinutes * 60);

            long editorSeconds = wcMgr.GetWcTimeInMinutes() * 60;

            _sessionSummary.currentDayKeystrokes += aggregate.keystrokes;
            _sessionSummary.currentDayLinesAdded += aggregate.linesAdded;
            _sessionSummary.currentDayLinesRemoved += aggregate.linesRemoved;

            SaveSessionSummaryToDisk(_sessionSummary);
        }

        private long GetMinutesSinceLastPayload()
        {
            long minutesSinceLastPayload = 1;
            long lastPayloadEnd = SoftwareCoUtil.getItemAsLong("latestPayloadTimestampEntUtc");
            if (lastPayloadEnd > 0)
            {
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                long diffInSec = nowTime.now - lastPayloadEnd;
                long sessionThresholdSeconds = 60 * 15;
                if (diffInSec > 0 && diffInSec <= sessionThresholdSeconds)
                {
                    minutesSinceLastPayload = diffInSec / 60;
                }
            }
            return minutesSinceLastPayload;

        }

        public void ÇlearSessionSummaryData()
        {
            _sessionSummary = new SessionSummary();
            SaveSessionSummaryToDisk(_sessionSummary);
        }

        public SessionSummary GetSessionSummayData()
        {
            if (!SoftwareCoUtil.SessionSummaryFileExists())
            {
                // create it
                SaveSessionSummaryToDisk(new SessionSummary());
            }

            string sessionSummary = SoftwareCoUtil.getSessionSummaryFileData();

            IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(sessionSummary);
            _sessionSummary = DictionaryToObject<SessionSummary>(jsonObj);
            return _sessionSummary;
        }

        private async Task<SessionSummaryResult> GetSessionSummaryStatusAsync(bool forceRefresh = false)
        {
            SessionSummaryResult sessionSummaryResult = new SessionSummaryResult();
            _sessionSummary = GetSessionSummayData();
            sessionSummaryResult.sessionSummary = _sessionSummary;
            sessionSummaryResult.status = "OK";
            return sessionSummaryResult;
        }

        public void SaveSessionSummaryToDisk(SessionSummary sessionSummary)
        {
            string MethodName = "saveSessionSummaryToDisk";
            string sessionSummaryFile = SoftwareCoUtil.getSessionSummaryFile();


            if (SoftwareCoUtil.SessionSummaryFileExists())
            {
                File.SetAttributes(sessionSummaryFile, FileAttributes.Normal);
            }

            try
            {
                //SoftwareCoUtil.WriteToFileThreadSafe(sessionSummary.GetSessionSummaryAsJson(), sessionSummaryFile);
                File.WriteAllText(sessionSummaryFile, sessionSummary.GetSessionSummaryAsJson(), System.Text.Encoding.UTF8);
                //File.SetAttributes(sessionSummaryFile, FileAttributes.ReadOnly);
            }
            catch (Exception e)
            {
                //
            }

        }

        private T DictionaryToObject<T>(IDictionary<string, object> dict) where T : new()
        {
            var t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                KeyValuePair<string, object> item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));

                // Find which property type (int, string, double? etc) the CURRENT property is...
                Type tPropertyType = t.GetType().GetProperty(property.Name).PropertyType;

                // Fix nullables...
                Type newT = Nullable.GetUnderlyingType(tPropertyType) ?? tPropertyType;

                // ...and change the type
                object newA = Convert.ChangeType(item.Value, newT);
                t.GetType().GetProperty(property.Name).SetValue(t, newA, null);
            }
            return t;
        }

        public void UpdateStatusBarWithSummaryData()
        {
            string MethodName = "updateStatusBarWithSummaryData";

            string iconName = "";
            string currentDayMinutesTime = "";
            _sessionSummary = GetSessionSummayData();
            long currentDayMinutesVal = _sessionSummary.currentDayMinutes;
            long averageDailyMinutesVal = _sessionSummary.averageDailyMinutes;

            currentDayMinutesTime = SoftwareCoUtil.HumanizeMinutes(currentDayMinutesVal);
            // string averageDailyMinutesTime = SoftwareCoUtil.HumanizeMinutes(averageDailyMinutesVal);

            // Code time today:  4 hrs | Avg: 3 hrs 28 min
            iconName = currentDayMinutesVal > averageDailyMinutesVal ? "rocket.png" : "cpaw.png";
            // string msg = string.Format("{0}{1}", inFlowIcon, currentDayMinutesTime);

            SoftwareCoUtil.UpdateStatusBarButtonText(currentDayMinutesTime, iconName);

        }

        internal class SessionSummaryResult
        {
            public SessionSummary sessionSummary { get; set; }
            public string status { get; set; }
        }
    }
}
