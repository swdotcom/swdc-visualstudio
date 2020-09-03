using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class WallclockManager
    {

        private static Timer timer;
        private static int ONE_MINUTE_SECONDS = 60;
        private static int ONE_MINUTE_MILLIS = 1000 * 60;

        private static long _wctime = 0;
        private static string _currentDay = "";

        public CancellationToken DisposalToken { get; private set; }

        public static void Initialize()
        {
            // fetch the current day from the sessions.json
            _currentDay = FileManager.getItemAsString("currentDay");
            // start the wall clock timer in 10 seconds, every 1 minute
            timer = new Timer(
                      WallclcockTimerHandlerAsync,
                      null,
                      10000,
                      ONE_MINUTE_MILLIS);

            DispatchUpdatesProcessorAsync();
        }

        public static void Dispose()
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }
        }

        private static void WallclcockTimerHandlerAsync(object stateinfo)
        {
            if (ApplicationIsActivated() || DocEventManager.Instance.hasData())
            {
                _wctime = FileManager.getItemAsLong("wctime");
                _wctime += ONE_MINUTE_SECONDS;
                FileManager.setNumericItem("wctime", _wctime);

                // update the file info file (async is fine)
                TimeDataManager.Instance.UpdateEditorSeconds(ONE_MINUTE_SECONDS);

                GetNewDayCheckerAsync();
            }
            DispatchUpdatesProcessorAsync();
        }

        private static bool ApplicationIsActivated()
        {
            var activatedHandle = GetForegroundWindow();
            if (activatedHandle == IntPtr.Zero)
            {
                return false; // No window is currently activated
            }

            var procId = Process.GetCurrentProcess().Id;
            int activeProcId;
            GetWindowThreadProcessId(activatedHandle, out activeProcId);

            return activeProcId == procId;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        public static long GetWcTimeInMinutes()
        {
            _wctime = FileManager.getItemAsLong("wctime");
            return _wctime / 60;
        }

        public static void ClearWcTime()
        {
            _wctime = 0L;
            FileManager.setNumericItem("wctime", _wctime);
        }

        private static async Task DispatchUpdatesProcessorAsync()
        {
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
            PackageManager.RebuildCodeMetricsAsync();
            PackageManager.RebuildGitMetricsAsync();
        }

        public static async Task GetNewDayCheckerAsync()
        {
            if (SoftwareCoUtil.IsNewDay())
            {
                SessionSummaryManager.Instance.ÇlearSessionSummaryData();

                // send the offline data
                SoftwareCoPackage.SendOfflinePluginBatchData();

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
                UpdateSessionSummaryFromServerAsync(true);

            }
        }

        public static async Task UpdateSessionSummaryFromServerAsync(bool useCurrentDayMetrics)
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
                    IDictionary<string, object> jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                    if (jsonObj != null)
                    {
                        try
                        {
                            SessionSummary incomingSummary = summary.GetSessionSummaryFromDictionary(jsonObj, useCurrentDayMetrics);
                            summary.CloneSessionSummary(incomingSummary);
                            SessionSummaryManager.Instance.SaveSessionSummaryToDisk(summary);

                            DispatchUpdatesProcessorAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.Error("failed to read json: " + e.Message);
                        }
                    }
                }
                PackageManager.RebuildMenuButtonsAsync();
            }
        }
    }
}

