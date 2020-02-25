using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;

namespace SoftwareCo
{
    public sealed class WallclockManager
    {
        private static readonly Lazy<WallclockManager> lazy = new Lazy<WallclockManager>(() => new WallclockManager());

        private System.Threading.Timer timer;
        private static int SECONDS_TO_INCREMENT = 30;
        private static int THIRTY_SECONDS_IN_MILLIS = 1000 * SECONDS_TO_INCREMENT;
        private static int ONE_MINUTE = THIRTY_SECONDS_IN_MILLIS * 2;

        private long _wctime = 0;
        private string _currentDay = "";
        private SoftwareCoPackage package;
        private SessionSummaryManager sessionSummaryMgr;

        public static WallclockManager Instance { get { return lazy.Value; } }

        private WallclockManager()
        {
            timer = new System.Threading.Timer(
                      WallclcockTimerHandler,
                      null,
                      THIRTY_SECONDS_IN_MILLIS,
                      THIRTY_SECONDS_IN_MILLIS);
            sessionSummaryMgr = SessionSummaryManager.Instance;
        }

        private void WallclcockTimerHandler(object stateinfo)
        {
            this._wctime = SoftwareCoUtil.getItemAsLong("wctime");
            this._wctime += SECONDS_TO_INCREMENT;
            SoftwareCoUtil.setNumericItem("wctime", this._wctime);
            DispatchUpdateAsync();
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }

        public long GetWcTimeInMinutes()
        {
            return this._wctime / 60;
        }

        public void ClearWcTime()
        {
            this._wctime = 0L;
            SoftwareCoUtil.setItem("wctime", this._wctime.ToString());
            DispatchUpdateAsync();
        }

        private async Task DispatchUpdateAsync()
        {
            package.RebuildCodeMetricsAsync();
        }

        public void UpdateBasedOnSessionSeconds(long session_seconds)
        {

            // check to see if the session seconds has gained before the editor seconds
            // if so, then update the editor seconds
            if (this._wctime < session_seconds)
            {
                this._wctime = session_seconds + 1;
                SoftwareCoUtil.setItem("wctime", this._wctime.ToString());

                // update the code metrics part of the tree since this value has changed
                DispatchUpdateAsync();
            }
        }

        private async Task GetNewDayChecker()
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            if (!nowTime.local_day.Equals(_currentDay))
            {
                // send the offline data
                SoftwareCoPackage.SendOfflineData(null);

                // send the offline TimeData payloads
                // await payloadMgr.sendOfflineTimeData();

                // day does't match. clear the wall clock time,
                // the session summary, time data summary,
                // and the file change info summary data
                ClearWcTime();
                // clearTimeDataSummary();
                SessionSummaryManager.Instance.ÇlearSessionSummaryData();
                // clearFileChangeInfoSummaryData();

                // set the current day
                _currentDay = nowTime.local_day;

                // update the current day
                SoftwareCoUtil.setItem("currentDay", _currentDay);
                // update the last payload timestamp
                long latestPayloadTimestampEndUtc = 0;
                SoftwareCoUtil.setNumericItem("latestPayloadTimestampEndUtc", latestPayloadTimestampEndUtc);

                // update the session summary global and averages for the new day
                Task.Delay(ONE_MINUTE).ContinueWith((task) => { UpdateSessionSummaryFromServerAsync(); });
            }
        }

        public async Task UpdateSessionSummaryFromServerAsync()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            if (jwt != null)
            {
                string api = "/sessions/summary";
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, jwt.ToString());
                if (SoftwareHttpManager.IsOk(response))
                {
                    SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                    if (jsonObj != null)
                    {
                        SessionSummary incomingSummary = SoftwareCoUtil.DictionaryToObject<SessionSummary>(jsonObj);
                        summary.CloneSessionSummary(incomingSummary);
                        SessionSummaryManager.Instance.SaveSessionSummaryToDisk(summary);

                        // update the wallclock time if the session seconds is greater. this can happen when using multiple editor types
                        WallclockManager.Instance.UpdateBasedOnSessionSeconds(summary.currentDayMinutes * 60);
                    }
                }
            }
        }
    }
}

