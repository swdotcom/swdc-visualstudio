using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{
    class SoftwareStatus
    {
        private string _lastStatusMsg = "";
        private IVsStatusbar statusbar;
        private bool showStatusText = true;
        private string lastMsg = "";
        private string lastTooltip = "";

        public SoftwareStatus(IVsStatusbar statusbar)
        {
            this.statusbar = statusbar;
        }

        public void ToggleStatusInfo()
        {
            showStatusText = !showStatusText;

            if (showStatusText)
            {
                SetStatus(lastMsg);
            }
            else
            {
                SetStatus("");
            }
        }

        public void SetStatus(string msg)
        {
            if (showStatusText)
            {
                lastMsg = msg;
            } else
            {
                // make sure the message is the clock
                msg = " 🕒 ";
            }
            if (statusbar == null)
            {
                return;
            }
            
            statusbar.SetText(msg);
            this._lastStatusMsg = msg;
        }

        public void ReloadStatus()
        {
            if (_lastStatusMsg != null)
            {
                this.SetStatus(_lastStatusMsg);
            }
        }
    }
}
