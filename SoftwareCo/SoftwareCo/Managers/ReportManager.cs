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

        private static int DASHBOARD_LABEL_WIDTH = 28;
        private static int DASHBOARD_VALUE_WIDTH = 36;
        private static int DASHBOARD_COL_WIDTH = 21;
        private static int DASHBOARD_LRG_COL_WIDTH = 38;
        private static int TABLE_WIDTH = 80;

        private static SimpleDateFormat formatDayTime = new SimpleDateFormat("EEE, MMM d h:mma");
        private static SimpleDateFormat formatDayYear = new SimpleDateFormat("MMM d, YYYY");

        private ReportManager()
        {

        }

        public async Task DisplayProjectContributorSummaryDashboard(string identifier)
        {
            StringBuilder sb = new StringBuilder();

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

                String lastUpdatedStr = formatDayTime.format(new Date());
                sb.Append(getTableHeader("PROJECT SUMMARY", " (Last updated on " + lastUpdatedStr + ")", true));
                sb.Append("\n\n Project: ").Append(identifier).append("\n\n");

                // TODAY
                String projectDate = formatDayYear.format(timesData.local_start_today_date);
                sb.Append(getRightAlignedTableHeader("Today (" + projectDate + ")"));
                sb.Append(getColumnHeaders(Arrays.asList("Metric", "You", "All Contributors")));
                sb.Append(getRowNumberData("Commits", usersTodaysCommits.getCommitCount(), contribTodaysCommits.getCommitCount()));
                sb.Append(getRowNumberData("Files changed", usersTodaysCommits.getFileCount(), contribTodaysCommits.getFileCount()));
                sb.Append(getRowNumberData("Insertions", usersTodaysCommits.getInsertions(), contribTodaysCommits.getInsertions()));
                sb.Append(getRowNumberData("Deletions", usersTodaysCommits.getDeletions(), contribTodaysCommits.getDeletions()));
                sb.Append("\n");

                // YESTERDAY
                String yesterday = formatDayYear.format(timesData.local_start_of_yesterday_date);
                sb.Append(getRightAlignedTableHeader("Yesterday (" + yesterday + ")"));
                sb.Append(getColumnHeaders(Arrays.asList("Metric", "You", "All Contributors")));
                sb.Append(getRowNumberData("Commits", usersYesterdaysCommits.getCommitCount(), contribYesterdaysCommits.getCommitCount()));
                sb.Append(getRowNumberData("Files changed", usersYesterdaysCommits.getFileCount(), contribYesterdaysCommits.getFileCount()));
                sb.Append(getRowNumberData("Insertions", usersYesterdaysCommits.getInsertions(), contribYesterdaysCommits.getInsertions()));
                sb.Append(getRowNumberData("Deletions", usersYesterdaysCommits.getDeletions(), contribYesterdaysCommits.getDeletions()));
                sb.Append("\n");

                // THIS WEEK
                String startOfWeek = formatDayYear.format(timesData.local_start_of_week_date);
                sb.Append(getRightAlignedTableHeader("This week (" + startOfWeek + " to " + projectDate + ")"));
                sb.Append(getColumnHeaders(Arrays.asList("Metric", "You", "All Contributors")));
                sb.Append(getRowNumberData("Commits", usersThisWeeksCommits.getCommitCount(), contribThisWeeksCommits.getCommitCount()));
                sb.Append(getRowNumberData("Files changed", usersThisWeeksCommits.getFileCount(), contribThisWeeksCommits.getFileCount()));
                sb.Append(getRowNumberData("Insertions", usersThisWeeksCommits.getInsertions(), contribThisWeeksCommits.getInsertions()));
                sb.Append(getRowNumberData("Deletions", usersThisWeeksCommits.getDeletions(), contribThisWeeksCommits.getDeletions()));
                sb.Append("\n");
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

        private static string getRowNumberData(string title, long userStat, long contribStat)
        {
            string userStatStr = SoftwareCoUtil.FormatNumber(userStat);
            string contribStatStr = SoftwareCoUtil.FormatNumber(contribStat);

            List<string> labels = new List<string>();
            labels.Add(title);
            labels.Add(userStatStr);
            labels.Add(contribStatStr);
            return getRowLabels(labels);
        }

        private static string getSpaces(int spacesRequired)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < spacesRequired; i++)
            {
                sb.Append(" ");
            }
            return sb.ToString();
        }

        private static string getBorder(int borderLen)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < borderLen; i++)
            {
                sb.Append("-");
            }
            sb.Append("\n");
            return sb.ToString();
        }

        private static string getDashboardRow(string label, string value, bool isSectionHeader)
        {
            int spacesRequired = DASHBOARD_LABEL_WIDTH - label.Length;
            string spaces = getSpaces(spacesRequired);
            string dashboardVal = getDashboardValue(value, isSectionHeader);
            StringBuilder sb = new StringBuilder();
            sb.Append(label).Append(spaces).Append(dashboardVal).Append("\n");
            int currLen = sb.Length;
            sb.Append(getBorder(currLen));
            return sb.ToString();
        }

        private static string getDashboardValue(string value, bool isSectionHeader)
        {
            Int32 spacesRequired = DASHBOARD_VALUE_WIDTH - value.Length - 2;
            string spaces = getSpaces(spacesRequired);
            if (!isSectionHeader)
            {
                // show the : divider
                return ": " + spaces + "" + value;
            }
            // we won't show the column divider
            return "  " + spaces + "" + value;
        }

        private static string getDashboardBottomBorder()
        {
            Int32 borderLen = DASHBOARD_LABEL_WIDTH + DASHBOARD_VALUE_WIDTH;
            string border = getBorder(borderLen);
            // add an additional newline
            return border + "\n";
        }

        private static string getSectionHeader(string label)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(label).Append("\n");
            Int32 borderLen = DASHBOARD_LABEL_WIDTH + DASHBOARD_VALUE_WIDTH;
            sb.Append(getBorder(borderLen));
            return sb.ToString();
        }

        private static string getRightAlignedTableLabel(string label, Int32 colWidth)
        {
            Int32 spacesRequired = colWidth - label.Length;
            string spaces = getSpaces(spacesRequired);
            return spaces + "" + label;
        }

        private static string getTableHeader(string leftLabel, string rightLabel, bool isFullTable)
        {
            Int32 fullLen = !isFullTable ? TABLE_WIDTH - DASHBOARD_COL_WIDTH : TABLE_WIDTH;
            Int32 spacesRequired = fullLen - leftLabel.Length - rightLabel.Length;
            string spaces = getSpaces(spacesRequired);
            return leftLabel + "" + spaces + "" + rightLabel;
        }

        private static string getRightAlignedTableHeader(string label)
        {
            StringBuilder sb = new StringBuilder();
            string alignedHeader = getRightAlignedTableLabel(label, TABLE_WIDTH);
            sb.Append(alignedHeader).Append("\n");
            sb.Append(getBorder(TABLE_WIDTH));
            return sb.ToString();
        }

        private static string getRowLabels(List<string> labels)
        {
            StringBuilder sb = new StringBuilder();
            Int32 spacesRequired = 0;
            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i] ;
                if (i == 0)
                {
                    sb.Append(label);
                    spacesRequired = DASHBOARD_COL_WIDTH - sb.Length - 1;
                    sb.Append(getSpaces(spacesRequired)).Append(":");
                }
                else if (i == 1)
                {
                    spacesRequired = DASHBOARD_LRG_COL_WIDTH + DASHBOARD_COL_WIDTH - sb.Length - label.Length - 1;
                    sb.Append(getSpaces(spacesRequired)).Append(label).Append(" ");
                }
                else
                {
                    spacesRequired = DASHBOARD_COL_WIDTH - label.Length - 2;
                    sb.Append("| ").Append(getSpaces(spacesRequired)).Append(label);
                }
            }
            sb.Append("\n");
            return sb.ToString();
        }

        private static string getColumnHeaders(List<string> labels)
        {
            StringBuilder sb = new StringBuilder();
            Int32 spacesRequired = 0;
            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (i == 0)
                {
                    sb.Append(label);
                }
                else if (i == 1)
                {
                    spacesRequired = DASHBOARD_LRG_COL_WIDTH + DASHBOARD_COL_WIDTH - sb.Length - label.Length - 1;
                    sb.Append(getSpaces(spacesRequired)).Append(label).Append(" ");
                }
                else
                {
                    spacesRequired = DASHBOARD_COL_WIDTH - label.Length - 2;
                    sb.Append("| ").Append(getSpaces(spacesRequired)).Append(label);
                }
            }
            sb.Append("\n");
            sb.Append(getBorder(TABLE_WIDTH));
            return sb.ToString();
        }
    }
}
