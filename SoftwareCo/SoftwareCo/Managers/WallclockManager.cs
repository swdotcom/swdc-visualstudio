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
    private static int FIVE_MINUTES_MILLIS = 1000 * 60 * 5;

    private static long _wctime = 0;

    public CancellationToken DisposalToken { get; private set; }

    public static void Initialize()
    {
      // start the wall clock timer in 10 seconds, every 1 minute
      timer = new Timer(
                WallclcockTimerHandlerAsync,
                null,
                1000,
                FIVE_MINUTES_MILLIS);
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
      bool isWinActivated = ApplicationIsActivated();

      if (!isWinActivated)
      {
        DocEventManager.Instance.PostData();
      }

      UpdateSessionSummaryFromServerAsync();

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

    public static async Task UpdateSessionSummaryFromServerAsync()
    {
      object jwt = FileManager.getItem("jwt");
      if (jwt != null)
      {
        string api = "/sessions/summary";
        HttpResponseMessage response = await SoftwareHttpManager.MetricsRequest(HttpMethod.Get, api);
        if (SoftwareHttpManager.IsOk(response))
        {
          SessionSummary summary = SessionSummaryManager.Instance.GetSessionSummayData();
          string responseBody = await response.Content.ReadAsStringAsync();
          responseBody = SoftwareCoUtil.CleanJsonToDeserialize(responseBody);
          IDictionary<string, object> jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
          if (jsonObj != null)
          {
            try
            {
              SessionSummary incomingSummary = summary.GetSessionSummaryFromDictionary(jsonObj);

              summary.CloneSessionSummary(incomingSummary);
              SessionSummaryManager.Instance.SaveSessionSummaryToDisk(summary);

              SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
            }
            catch (Exception e)
            {
              Logger.Error("error reading file: " + e.Message);
            }
          }
        }
      }
    }
  }
}

