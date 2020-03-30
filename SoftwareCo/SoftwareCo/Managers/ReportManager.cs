using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SoftwareCo
{
    public class ReportManager
    {
        private static readonly Lazy<ReportManager> lazy = new Lazy<ReportManager>(() => new ReportManager());

        public static ReportManager Instance { get { return lazy.Value; } }

        private ReportManager()
        {

        }

        public async Task DisplayProjectContributorSummaryDashboard(string identifier)
        {
            // fetch the git stats
            string projectDir = DocEventManager._solutionDirectory;
            if (projectDir != null)
            {
                NowTime nowTime = SoftwareCoUtil.GetNowTime();
                string email = GitUtilManager.GetUsersEmail(projectDir);
                CommitChangeStats usersTodaysCommits = GitUtilManager.GetTodaysCommits(projectDir, email);
                CommitChangeStats contribTodaysCommits = GitUtilManager.GetTodaysCommits(projectDir, null);

                CommitChangeStats usersYesterdaysCommits = GitUtilManager.GetYesterdayCommits(projectDir, email);
                CommitChangeStats contribYesterdaysCommits = GitUtilManager.GetYesterdayCommits(projectDir, null);

                CommitChangeStats usersThisWeeksCommits = GitUtilManager.GetThisWeeksCommits(projectDir, email);
                CommitChangeStats contribThisWeeksCommits = GitUtilManager.GetThisWeeksCommits(projectDir, null);
            }
            
            try
            {
                string dashboardFile = SoftwareCoUtil.GetContributorDashboardFile();
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
