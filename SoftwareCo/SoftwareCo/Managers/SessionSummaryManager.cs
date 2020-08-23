using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using System.IO;

namespace SoftwareCo
{
    public sealed class SessionSummaryManager
    {
        private static readonly Lazy<SessionSummaryManager> lazy = new Lazy<SessionSummaryManager>(() => new SessionSummaryManager());

        private SessionSummary _sessionSummary;
        private SoftwareCoPackage package;

        public static SessionSummaryManager Instance { get { return lazy.Value; } }

        private SessionSummaryManager()
        {
            // initialize the session summary
            GetSessionSummayData();
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }
        
        public void IncrementSessionSummaryData(KeystrokeAggregates aggregate, TimeGapData eTimeInfo)
        {
            _sessionSummary = GetSessionSummayData();

            if (eTimeInfo.session_seconds > 0)
            {
                _sessionSummary.currentDayMinutes += (eTimeInfo.session_seconds / 60);
            }

            long sessionSeconds = _sessionSummary.currentDayMinutes * 60;

            _sessionSummary.currentDayKeystrokes += aggregate.keystrokes;
            _sessionSummary.currentDayLinesAdded += aggregate.linesAdded;
            _sessionSummary.currentDayLinesRemoved += aggregate.linesRemoved;

            SaveSessionSummaryToDisk(_sessionSummary);
        }

        public TimeGapData GetTimeBetweenLastPayload()
        {
            TimeGapData eTimeInfo = new TimeGapData();
            long sessionSeconds = 60;
            long elapsedSeconds = 60;

            long lastPayloadEnd = FileManager.getItemAsLong("latestPayloadTimestampEndUtc");
            if (lastPayloadEnd > 0)
            {
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                elapsedSeconds = Math.Max(60, nowTime.now - lastPayloadEnd);
                long sessionThresholdSeconds = 60 * 15;
                if (elapsedSeconds > 0 && elapsedSeconds <= sessionThresholdSeconds)
                {
                    sessionSeconds = elapsedSeconds;
                }
                sessionSeconds = Math.Max(60, sessionSeconds);
            }
            eTimeInfo.elapsed_seconds = elapsedSeconds;
            eTimeInfo.session_seconds = sessionSeconds;
            return eTimeInfo;
        }

        public void ÇlearSessionSummaryData()
        {
            _sessionSummary = new SessionSummary();
            SaveSessionSummaryToDisk(_sessionSummary);
        }

        public SessionSummary GetSessionSummayData()
        {
            if (!FileManager.SessionSummaryFileExists())
            {
                // create it
                SaveSessionSummaryToDisk(new SessionSummary());
            }

            string sessionSummary = FileManager.getSessionSummaryFileData();

            IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(sessionSummary, new Dictionary<string, object>());
            _sessionSummary = new SessionSummary();
            _sessionSummary = _sessionSummary.GetSessionSummaryFromDictionary(jsonObj);
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
            string sessionSummaryFile = FileManager.getSessionSummaryFile();

            if (FileManager.SessionSummaryFileExists())
            {
                File.SetAttributes(sessionSummaryFile, FileAttributes.Normal);
            }

            try
            {
                string content = SimpleJson.SerializeObject(sessionSummary.GetSessionSummaryDict());
                content = content.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);
                File.WriteAllText(sessionSummaryFile, content, System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }

        }

        public async Task UpdateStatusBarWithSummaryDataAsync()
        {
            string MethodName = "UpdateStatusBarWithSummaryDataAsync";

            CodeTimeSummary ctSummary = TimeDataManager.Instance.GetCodeTimeSummary();

            string iconName = "";
            string currentDayMinutesTime = "";
            _sessionSummary = GetSessionSummayData();
            long averageDailyMinutesVal = _sessionSummary.averageDailyMinutes;

            currentDayMinutesTime = SoftwareCoUtil.HumanizeMinutes(ctSummary.activeCodeTimeMinutes);
            // string averageDailyMinutesTime = SoftwareCoUtil.HumanizeMinutes(averageDailyMinutesVal);

            // Code time today:  4 hrs | Avg: 3 hrs 28 min
            iconName = ctSummary.activeCodeTimeMinutes > averageDailyMinutesVal ? "rocket.png" : "cpaw.png";
            // string msg = string.Format("{0}{1}", inFlowIcon, currentDayMinutesTime);

            // it's ok not to await on this
            PackageManager.UpdateStatusBarButtonText(currentDayMinutesTime, iconName);
        }

        internal class SessionSummaryResult
        {
            public SessionSummary sessionSummary { get; set; }
            public string status { get; set; }
        }
    }
}
