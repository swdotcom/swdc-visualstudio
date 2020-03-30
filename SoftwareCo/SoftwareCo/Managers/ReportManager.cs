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

        private ReportManager()
        {
        }

        public async Task DisplayProjectContributorSummaryDashboard()
        {
            string file = SoftwareCoUtil.GetContributorDashboardFile();
            StringBuilder sb = new StringBuilder();

            // fetch the git stats
            string projectDir = DocEventManager._solutionDirectory;

            RepoResourceInfo resourceInfo = GitUtilManager.GetResourceInfo(projectDir, false);
            string identifier = resourceInfo != null && resourceInfo.identifier != null ? resourceInfo.identifier : "Untitled";

            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            string email = GitUtilManager.GetUsersEmail(projectDir);
            CommitChangeStats usersTodaysCommits = GitUtilManager.GetTodaysCommits(projectDir, email);
            CommitChangeStats contribTodaysCommits = GitUtilManager.GetTodaysCommits(projectDir, null);

            CommitChangeStats usersYesterdaysCommits = GitUtilManager.GetYesterdayCommits(projectDir, email);
            CommitChangeStats contribYesterdaysCommits = GitUtilManager.GetYesterdayCommits(projectDir, null);

            CommitChangeStats usersThisWeeksCommits = GitUtilManager.GetThisWeeksCommits(projectDir, email);
            CommitChangeStats contribThisWeeksCommits = GitUtilManager.GetThisWeeksCommits(projectDir, null);


            string lastUpdatedStr = DateTime.Now.ToString("dddd, MMM d h:mm tt");
            sb.Append(getTableHeader("PROJECT SUMMARY", " (Last updated on " + lastUpdatedStr + ")", true));
            sb.Append("\n\n Project: ").Append(identifier).Append("\n\n");

            // TODAY
            String projectDate = DateTime.Now.ToString("MMM d, yyyy");
            sb.Append(getRightAlignedTableHeader("Today (" + projectDate + ")"));
            sb.Append(getColumnHeaders(new List<string>() { "Metric", "You", "All Contributors" }));
            sb.Append(getRowNumberData("Commits", usersTodaysCommits.commitCount, contribTodaysCommits.commitCount));
            sb.Append(getRowNumberData("Files changed", usersTodaysCommits.fileCount, contribTodaysCommits.fileCount));
            sb.Append(getRowNumberData("Insertions", usersTodaysCommits.insertions, contribTodaysCommits.insertions));
            sb.Append(getRowNumberData("Deletions", usersTodaysCommits.deletions, contribTodaysCommits.deletions));
            sb.Append("\n");

            // YESTERDAY
            String yesterday = nowTime.start_of_yesterday_dt.ToString("MMM d, yyyy");
            sb.Append(getRightAlignedTableHeader("Yesterday (" + yesterday + ")"));
            sb.Append(getColumnHeaders(new List<string>() { "Metric", "You", "All Contributors" }));
            sb.Append(getRowNumberData("Commits", usersYesterdaysCommits.commitCount, contribYesterdaysCommits.commitCount));
            sb.Append(getRowNumberData("Files changed", usersYesterdaysCommits.fileCount, contribYesterdaysCommits.fileCount));
            sb.Append(getRowNumberData("Insertions", usersYesterdaysCommits.insertions, contribYesterdaysCommits.insertions));
            sb.Append(getRowNumberData("Deletions", usersYesterdaysCommits.deletions, contribYesterdaysCommits.deletions));
            sb.Append("\n");

            // THIS WEEK
            String startOfWeek = nowTime.start_of_week_dt.ToString("MMM d, yyyy");
            sb.Append(getRightAlignedTableHeader("This week (" + startOfWeek + " to " + projectDate + ")"));
            sb.Append(getColumnHeaders(new List<string>() { "Metric", "You", "All Contributors" }));
            sb.Append(getRowNumberData("Commits", usersThisWeeksCommits.commitCount, contribThisWeeksCommits.commitCount));
            sb.Append(getRowNumberData("Files changed", usersThisWeeksCommits.fileCount, contribThisWeeksCommits.fileCount));
            sb.Append(getRowNumberData("Insertions", usersThisWeeksCommits.insertions, contribThisWeeksCommits.insertions));
            sb.Append(getRowNumberData("Deletions", usersThisWeeksCommits.deletions, contribThisWeeksCommits.deletions));
            sb.Append("\n");


            if (File.Exists(file))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            try
            {
                File.WriteAllText(file, sb.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {

            }

            try
            {
                
                if (File.Exists(file))
                {
                    SoftwareCoPackage.ObjDte.ItemOperations.OpenFile(file);
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
