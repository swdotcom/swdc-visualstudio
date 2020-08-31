namespace SoftwareCo
{
    using Microsoft.VisualStudio.Shell;
    using System;
    using System.Runtime.InteropServices;

    [Guid("B9ADECFD-3D3C-451D-AE3A-90994DB55AA4")]
    public class CodeMetricsToolPane : ToolWindowPane
    {
        private long lastMetricsRebuild = 0;
        private long lastMenuRebuild = 0;
        private long lastGitMetricsRebuild = 0;

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
            if (this.Content != null && SoftwareCoPackage.INITIALIZED)
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (now - lastMenuRebuild > 5)
                {
                    ((CodeMetricsTree)this.Content).RebuildMenuButtonsAsync();
                    lastMenuRebuild = now;
                }
            }
        }

        public void RebuildCodeMetrics()
        {
            if (this.Content != null && SoftwareCoPackage.INITIALIZED)
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (now - lastMetricsRebuild > 5)
                {
                    ((CodeMetricsTree)this.Content).RebuildCodeMetricsAsync();
                    lastMetricsRebuild = now;
                }
            }
        }

        public void ToggleClickHandler()
        {
            StatusBarButton.showingStatusbarMetrics = !StatusBarButton.showingStatusbarMetrics;
            if (this.Content != null && SoftwareCoPackage.INITIALIZED)
            {
                ((CodeMetricsTree)this.Content).RebuildCodeMetricsAsync();
            }
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryDataAsync();
        }

        public void RebuildGitMetricsAsync()
        {
            if (this.Content != null && SoftwareCoPackage.INITIALIZED)
            {
                long now = DateTimeOffset.Now.ToUnixTimeSeconds();
                if (now - lastGitMetricsRebuild > 5)
                {
                    ((CodeMetricsTree)this.Content).RebuildGitMetricsAsync();
                    ((CodeMetricsTree)this.Content).RebuildContributorMetricsAsync();
                    lastGitMetricsRebuild = now;
                }
            }
        }
    }
}
