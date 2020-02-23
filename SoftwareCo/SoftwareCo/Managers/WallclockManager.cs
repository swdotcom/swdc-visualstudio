using System;

namespace SoftwareCo
{
    public sealed class WallclockManager
    {
        private static readonly Lazy<WallclockManager> lazy = new Lazy<WallclockManager>(() => new WallclockManager());

        private System.Threading.Timer timer;
        private static int SECONDS_TO_INCREMENT = 30;
        private static int THIRTY_SECONDS_IN_MILLIS = 1000 * SECONDS_TO_INCREMENT;

        private long _wctime = 0;
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
            SoftwareCoUtil.setItem("_wctime", this._wctime.ToString());
        }

        private void DispatchStatusViewUpdate()
        {
            sessionSummaryMgr.UpdateStatusBarWithSummaryData();

            // refresh the tree
        }

        public void ClearWcTime()
        {
            this._wctime = 0L;
            SoftwareCoUtil.setItem("_wctime", this._wctime.ToString());
        }

        public void updateBasedOnSessionSeconds(long session_seconds)
        {

            // check to see if the session seconds has gained before the editor seconds
            // if so, then update the editor seconds
            if (this._wctime < session_seconds)
            {
                this._wctime = session_seconds + 1;
                SoftwareCoUtil.setItem("_wctime", this._wctime.ToString());

                // refresh the tree
            }
        }
    }
}

