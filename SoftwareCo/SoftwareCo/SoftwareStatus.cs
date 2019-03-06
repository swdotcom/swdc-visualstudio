using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{
    class SoftwareStatus
    {
        private string _lastStatusMsg = "";
        private IVsStatusbar statusbar;

        public SoftwareStatus(IVsStatusbar statusbar)
        {
            this.statusbar = statusbar;
        }

        public void SetStatus(string msg)
        {
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
