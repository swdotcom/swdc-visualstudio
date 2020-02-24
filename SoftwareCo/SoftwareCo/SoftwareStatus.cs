using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace SoftwareCo
{
    public class SoftwareStatus
    {
        private string _lastStatusMsg = "";
        private IVsStatusbar statusbar;
        private bool showStatusText = true;
        private string lastMsg = "";

        public SoftwareStatus(IVsStatusbar statusbar)
        {
            this.statusbar = statusbar;
        }

        public void ToggleStatusInfo()
        {
            showStatusText = !showStatusText;

            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryData();
        }

        public bool ShowingStatusText()
        {
            return showStatusText;
        }

    }
}
