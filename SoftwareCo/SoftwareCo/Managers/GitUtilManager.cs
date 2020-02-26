using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class GitUtilManager
    {
        private static readonly Lazy<GitUtilManager> lazy = new Lazy<GitUtilManager>(() => new GitUtilManager());

        private SoftwareCoPackage package;

        public static GitUtilManager Instance { get { return lazy.Value; } }

        private GitUtilManager()
        {
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }

        public CommitChangeStats GetUncommitedChanges(string projectDir)
        {
            string cmd = "git diff --stat";

            return GetChangeStats(cmd, projectDir); ;
        }

        public CommitChangeStats GetTodaysCommits(string projectDir)
        {
            if (projectDir == null || projectDir.Equals(""))
            {
                return new CommitChangeStats();
            }
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            RepoResourceInfo info = SoftwareCoUtil.GetResourceInfo(projectDir);
            
            string cmd = "git log --stat --pretty=\"COMMIT:% H,% ct,% cI,% s\" --since=" + nowTime.local_start_of_day;
            if (info != null && info.email != null && !info.email.Equals(""))
            {
                cmd += " --author=" + info.email;
            }
            return GetChangeStats(cmd, projectDir);
        }

        private CommitChangeStats GetChangeStats(string cmd, string projectDir)
        {
            if (projectDir == null || projectDir.Equals(""))
            {
                return new CommitChangeStats();
            }
            CommitChangeStats stats = new CommitChangeStats();

            /**
	         * example:
             * -mbp-2:swdc-vscode xavierluiz$ git diff --stat
                lib/KpmProviderManager.ts | 22 ++++++++++++++++++++--
                1 file changed, 20 insertions(+), 2 deletions(-)

                for multiple files it will look like this...
                7 files changed, 137 insertions(+), 55 deletions(-)
             */
            List<string> results = SoftwareCoUtil.GetCommandResultList(cmd, projectDir);

            if (results == null || results.Count == 0)
            {
                // something went wrong, but don't try to parse a null or undefined str
                return stats;
            }

            // just look for the line with "insertions" and "deletions"
            return AccumulateChangeStats(results);
        }

        public CommitChangeStats AccumulateChangeStats(List<string> results)
        {
            CommitChangeStats stats = new CommitChangeStats();

            if (results != null)
            {
                foreach (string line in results)
                {
                    string lineData = line.Trim();
                    lineData = Regex.Replace(lineData, @"\s+", " ");
                    // look for lines with insertion and deletion
                    if (lineData.IndexOf("insertion") != -1 || lineData.IndexOf("deletion") != -1)
                    {
                        string[] parts = lineData.Split(' ');
                        // the 1st element is the number of files changed
                        int fileCount = int.Parse(parts[0]);
                        stats.fileCount += fileCount;
                        stats.commitCount += 1;
                        for (int x = 0; x < parts.Count(); x++)
                        {
                            string part = parts[x];
                            if (part.IndexOf("insertion") != -1)
                            {
                                int insertions = int.Parse(parts[x - 1]);
                                stats.insertions += insertions;
                            } else if (part.IndexOf("deletion") != -1)
                            {
                                int deletions = int.Parse(parts[x - 1]);
                                stats.deletions += deletions;
                            }
                        }
                    }
                }
            }

            return stats;
        }
    }
}
