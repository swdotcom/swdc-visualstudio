using System;
using System.IO;
using System.Net.Http;
using Task = System.Threading.Tasks.Task;

namespace SoftwareCo
{
    public sealed class DashboardManager
    {
        private static readonly Lazy<DashboardManager> lazy = new Lazy<DashboardManager>(() => new DashboardManager());

        public static DashboardManager Instance { get { return lazy.Value; } }

        private DashboardManager()
        {

        }

        private static string NO_DATA = "CODE TIME\n\nNo data available\n";

        public async void LaunchCodeTimeDashboardAsync()
        {
            SoftwareCoUtil.launchCodeTimeDashboard();
        }

        public async void LaunchSettingsView()
        {
            SoftwareCoUtil.launchSettings();
        }

        public async void LaunchReadmeFileAsync()
        {
            SoftwareCoUtil.launchReadme();
        }
    }
}
