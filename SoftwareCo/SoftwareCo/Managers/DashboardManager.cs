using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Microsoft.VisualStudio;
using System.Windows.Forms;

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

        private async Task FetchCodeTimeDashboardAsync()
        {

            string summaryContent = "";
            string summaryInfoFile = SoftwareCoUtil.getSessionSummaryInfoFile();

            bool online = await SoftwareUserSession.IsOnlineAsync();


            HttpResponseMessage resp =
            await SoftwareHttpManager.SendDashboardRequestAsync(HttpMethod.Get, "/dashboard?showMusic=false&showGit=false&showRank=false&showToday=false");

            if (SoftwareHttpManager.IsOk(resp))
            {
                summaryContent += await resp.Content.ReadAsStringAsync();
            }
            else
            {
                summaryContent = NO_DATA;
            }


            if (File.Exists(summaryInfoFile))
            {
                File.SetAttributes(summaryInfoFile, FileAttributes.Normal);
            }

            try
            {

                File.WriteAllText(summaryInfoFile, summaryContent, System.Text.Encoding.UTF8);
                // File.SetAttributes(summaryInfoFile, FileAttributes.ReadOnly);
            }
            catch (Exception e)
            {

            }

            string dashboardFile = SoftwareCoUtil.getDashboardFile();
            string dashboardContent = "";
            string suffix = SoftwareCoUtil.CreateDateSuffix(DateTime.Now);
            string formattedDate = DateTime.Now.ToString("ddd, MMM ") + suffix + DateTime.Now.ToString(" h:mm tt");

            dashboardContent = "CODE TIME          " + "(Last updated on " + formattedDate + " )";
            dashboardContent += "\n\n";

            string todayDate = DateTime.Now.ToString("ddd, MMM ") + suffix;
            string today_date = "Today " + "(" + todayDate + ")";
            dashboardContent += SoftwareCoUtil.getSectionHeader(today_date);

            SessionSummary _sessionSummary = SessionSummaryManager.Instance.GetSessionSummayData();
            if (_sessionSummary != null)
            {

                string averageTime = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.averageDailyMinutes);
                string hoursCodedToday = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.currentDayMinutes);
                String liveshareTime = "";
                //if (_sessionSummary.liveshareMinutes != 0)
                //{
                //    liveshareTime = SoftwareCoUtil.HumanizeMinutes(_sessionSummary.liveshareMinutes);
                //}
                dashboardContent += SoftwareCoUtil.getDashboardRow("Hours Coded", hoursCodedToday);
                dashboardContent += SoftwareCoUtil.getDashboardRow("90-day avg", averageTime);
                //if (liveshareTime != "0")
                //{
                //    dashboardContent += SoftwareCoUtil.getDashboardRow("Live Share", liveshareTime);
                //}
                dashboardContent += "\n";
            }

            if (SoftwareCoUtil.SessionSummaryInfoFileExists())
            {
                string SummaryData = SoftwareCoUtil.getSessionSummaryInfoFileData();
                dashboardContent += SummaryData;
            }


            if (File.Exists(dashboardFile))
            {
                File.SetAttributes(dashboardFile, FileAttributes.Normal);
            }
            try
            {
                //SoftwareCoUtil.WriteToFileThreadSafe(dashboardContent, dashboardFile);
                File.WriteAllText(dashboardFile, dashboardContent, System.Text.Encoding.UTF8);
                // File.SetAttributes(dashboardFile, FileAttributes.ReadOnly);

            }
            catch (Exception e)
            {

            }

        }

        public async void LaunchCodeTimeDashboardAsync()
        {
            try
            {
                await FetchCodeTimeDashboardAsync();
                string dashboardFile = SoftwareCoUtil.getDashboardFile();
                if (File.Exists(dashboardFile))
                {
                    SoftwareCoPackage.ObjDte.ItemOperations.OpenFile(dashboardFile);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("LaunchCodeTimeDashboardAsync, error : " + ex.Message, ex);

            }

        }
    }
}
