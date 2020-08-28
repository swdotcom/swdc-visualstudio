namespace SoftwareCo
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Runtime.InteropServices;

    [Guid("B9ADECFD-3D3C-451D-AE3A-90994DB55AA4")]
    public class CodeMetricsToolPane : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeMetricsToolPane"/> class.
        /// </summary>
        public CodeMetricsToolPane() : base(null)
        {
            this.Caption = "CodeTime";

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new CodeMetricsTree();

        }

        public void RebuildMenuButtons()
        {
            if (this.Content != null)
            {
                ((CodeMetricsTree)this.Content).RebuildMenuButtonsAsync();
            }
        }

        public void RebuildCodeMetrics()
        {
            if (this.Content != null)
            {
                ((CodeMetricsTree)this.Content).RebuildCodeMetricsAsync();
            }
        }

        public void ToggleClickHandler()
        {
            StatusBarButton.showingStatusbarMetrics = !StatusBarButton.showingStatusbarMetrics;
            if (this.Content != null)
            {
                ((CodeMetricsTree)this.Content).RebuildCodeMetricsAsync();
            }
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
        }

        public void RebuildGitMetricsAsync()
        {
            if (this.Content != null)
            {
                ((CodeMetricsTree)this.Content).RebuildGitMetricsAsync();
                ((CodeMetricsTree)this.Content).RebuildContributorMetricsAsync();
            }
        }
    }
}
