using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class WallclockManager
    {
        private static readonly Lazy<WallclockManager> lazy = new Lazy<WallclockManager>(() => new WallclockManager());

        private System.Threading.Timer timer;
        private System.Threading.Timer newDayTimer;
        private static int SECONDS_TO_INCREMENT = 30;
        private static int THIRTY_SECONDS_IN_MILLIS = 1000 * SECONDS_TO_INCREMENT;
        private static int ONE_MINUTE = THIRTY_SECONDS_IN_MILLIS * 2;

        // 1 hour threshold
        private static long EDITOR_ACTIVE_THRESHOLD = 60 * 15;

        private long _wctime = 0;
        private string _currentDay = "";

        private SessionSummaryManager sessionSummaryMgr;

        public static WallclockManager Instance { get { return lazy.Value; } }

        public CancellationToken DisposalToken { get; private set; }

        private WallclockManager()
        {
            // fetch the current day from the sessions.json
            this._currentDay = FileManager.getItemAsString("currentDay");
            timer = new Timer(
                      WallclcockTimerHandlerAsync,
                      null,
                      1000,
                      THIRTY_SECONDS_IN_MILLIS);
            sessionSummaryMgr = SessionSummaryManager.Instance;

            newDayTimer = new Timer(
                    GetNewDayChecker,
                    null,
                    1000,
                    ONE_MINUTE * 10);
        }

        public void Dispose()
        {
            if (newDayTimer != null)
            {
                newDayTimer.Dispose();
                newDayTimer = null;
            }

            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private void WallclcockTimerHandlerAsync(object stateinfo)
        {
            if (IsVisualStudioAppInForeground() || DocEventManager.Instance.hasData())
            {
                _wctime = FileManager.getItemAsLong("wctime");
                _wctime += SECONDS_TO_INCREMENT;
                FileManager.setNumericItem("wctime", _wctime);

                // update the file info file (async is fine)
                TimeDataManager.Instance.UpdateEditorSeconds(SECONDS_TO_INCREMENT);
            }
            DispatchUpdateAsync();
        }

        public bool IsVisualStudioAppInForeground()
        {
            TimeGapData tgd = SessionSummaryManager.Instance.GetTimeBetweenLastPayload();
            return (tgd.elapsed_seconds < EDITOR_ACTIVE_THRESHOLD) ? true : false;
        }

        public long GetWcTimeInMinutes()
        {
            this._wctime = FileManager.getItemAsLong("wctime");
            return this._wctime / 60;
        }

        public void ClearWcTime()
        {
            this._wctime = 0L;
            FileManager.setNumericItem("wctime", this._wctime);
        }

        public async Task DispatchUpdateAsync()
        {
            Task.Delay(2000).ContinueWith((task) => { DispatchUpdatesProcessorAsync(); });
        }

        private async Task DispatchUpdatesProcessorAsync() {
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
            PackageManager.RebuildCodeMetricsAsync();
            PackageManager.RebuildGitMetricsAsync();
        }

        public async void GetNewDayChecker(object stateinfo)
        {
            GetNewDayCheckerAsync();
        }

        public async Task GetNewDayCheckerAsync() {
            if (SoftwareCoUtil.IsNewDay())
            {
                SessionSummaryManager.Instance.ÇlearSessionSummaryData();

                // send the offline data
                SoftwareCoPackage.SendOfflinePluginBatchData();

                // clear the last payload in memory
                FileManager.ClearLastSavedKeystrokeStats();

                // send the offline TimeData payloads
                // this will clear the time data summary as well
                TimeDataManager.Instance.SendTimeDataAsync();

                // day does't match. clear the wall clock time,
                // the session summary, time data summary,
                // and the file change info summary data
                ClearWcTime();

                // set the current day
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                _currentDay = nowTime.day;

                // update the current day
                FileManager.setItem("currentDay", _currentDay);
                // update the last payload timestamp
                FileManager.setNumericItem("latestPayloadTimestampEndUtc", 0);

                // update the session summary global and averages for the new day
                Task.Delay(ONE_MINUTE).ContinueWith((task) => { WallclockManager.Instance.UpdateSessionSummaryFromServerAsync(); });

            }
        }

        public async Task UpdateSessionSummaryFromServerAsync()
        {
            object jwt = FileManager.getItem("jwt");
            if (jwt != null)
            {
                string api = "/sessions/summary?refresh=true";
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api);
                if (SoftwareHttpManager.IsOk(response))
                {
                    SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                    if (jsonObj != null)
                    {
                        try
                        {
                            SessionSummary incomingSummary = summary.GetSessionSummaryFromDictionary(jsonObj);
                            summary.CloneSessionSummary(incomingSummary);
                            SessionSummaryManager.Instance.SaveSessionSummaryToDisk(summary);

                            DispatchUpdatesProcessorAsync();
                        } catch (Exception e)
                        {
                            Logger.Error("failed to read json: " + e.Message);
                        }
                    }

                    PackageManager.RebuildMenuButtonsAsync();
                }
            }
        }
    }
}

