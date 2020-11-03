using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class GitUtil
    {
        public static CommitChangeStats GetUncommitedChanges(string projectDir)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitChangeStats();
            }
            string cmd = "git diff --stat";

            return GetChangeStats(cmd, projectDir); ;
        }

        public static CommitChangeStats GetTodaysCommits(string projectDir, string email)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitChangeStats();
            }
            return GetCommitsForRange("today", projectDir, email);
        }

        public static CommitChangeStats GetYesterdayCommits(string projectDir, string email)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitChangeStats();
            }
            return GetCommitsForRange("yesterday", projectDir, email);
        }

        public static CommitChangeStats GetThisWeeksCommits(string projectDir, string email)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitChangeStats();
            }
            return GetCommitsForRange("thisWeek", projectDir, email);
        }

        public static CommitChangeStats GetCommitsForRange(string rangeType, string projectDir, string email)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitChangeStats();
            }
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            string sinceTime = nowTime.start_of_today.ToString("yyyy-MM-ddTHH:mm:sszzz");
            string untilTime = null;
            if (rangeType == "yesterday")
            {
                sinceTime = nowTime.start_of_yesterday_dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
                untilTime = nowTime.start_of_today.ToString("yyyy-MM-ddTHH:mm:sszzz");
            }
            else if (rangeType == "thisWeek")
            {
                sinceTime = nowTime.start_of_week_dt.ToString("yyyy-MM-ddTHH:mm:sszzz");
            }

            string cmd = "git log --stat --pretty=\"COMMIT:% H,% ct,% cI,% s\" --since=\"" + sinceTime + "\"";
            if (untilTime != null)
            {
                cmd += " --until=\"" + untilTime + "\"";
            }
            if (email != null && !email.Equals(""))
            {
                cmd += " --author=" + email;
            }
            return GetChangeStats(cmd, projectDir);
        }

        private static CommitChangeStats GetChangeStats(string cmd, string projectDir)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
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

        public static CommitChangeStats AccumulateChangeStats(List<string> results)
        {
            CommitChangeStats stats = new CommitChangeStats();

            if (results != null)
            {
                foreach (string line in results)
                {
                    string lineData = line.Trim();
                    lineData = Regex.Replace(lineData, @"\s+", " ");
                    // look for lines with insertion and deletion
                    if (lineData.IndexOf("changed") != -1 &&
                        (lineData.IndexOf("insertion") != -1 || lineData.IndexOf("deletion") != -1))
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
                            }
                            else if (part.IndexOf("deletion") != -1)
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

        public static RepoResourceInfo GetResourceInfo(string projectDir, bool includeMembers)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new RepoResourceInfo();
            }
            RepoResourceInfo info = new RepoResourceInfo();
            try
            {
                string identifier = SoftwareCoUtil.GetFirstCommandResult("git config remote.origin.url", projectDir);
                if (identifier != null && !identifier.Equals(""))
                {
                    info.identifier = identifier;

                    // only get these since identifier is available
                    string email = SoftwareCoUtil.GetFirstCommandResult("git config user.email", projectDir);
                    if (email != null && !email.Equals(""))
                    {
                        info.email = email;

                    }
                    string branch = SoftwareCoUtil.GetFirstCommandResult("git symbolic-ref --short HEAD", projectDir);
                    if (branch != null && !branch.Equals(""))
                    {
                        info.branch = branch;
                    }
                    string tag = SoftwareCoUtil.GetFirstCommandResult("git describe --all", projectDir);

                    if (tag != null && !tag.Equals(""))
                    {
                        info.tag = tag;
                    }

                    // get the ownerId and repoName from the identifier
                    string[] parts = identifier.Split('/');
                    if (parts.Length > 2)
                    {
                        string repoNamePart = parts[parts.Length - 1];
                        int typeIdx = repoNamePart.IndexOf(".git");
                        if (typeIdx != -1)
                        {
                            // it's a git identifier
                            info.ownerId = parts[parts.Length - 2];
                            info.repoName = repoNamePart.Substring(0, typeIdx);
                        }
                    }

                    if (includeMembers)
                    {
                        List<RepoMember> repoMembers = new List<RepoMember>();
                        string gitLogData = SoftwareCoUtil.GetFirstCommandResult("git log --pretty=%an,%ae | sort", projectDir);

                        IDictionary<string, string> memberMap = new Dictionary<string, string>();

                        if (gitLogData != null && !gitLogData.Equals(""))
                        {
                            string[] lines = gitLogData.Split(
                                new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            if (lines != null && lines.Length > 0)
                            {
                                for (int i = 0; i < lines.Length; i++)
                                {
                                    string line = lines[i];
                                    string[] memberInfos = line.Split(',');
                                    if (memberInfos != null && memberInfos.Length > 1)
                                    {
                                        string name = memberInfos[0].Trim();
                                        string memberEmail = memberInfos[1].Trim();
                                        if (!memberMap.ContainsKey(email))
                                        {
                                            memberMap.Add(email, name);
                                            repoMembers.Add(new RepoMember(name, email));
                                        }
                                    }
                                }
                            }
                        }
                        info.members = repoMembers;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetResourceInfo , error :" + ex.Message, ex);

            }
            return info;
        }

        public static string GetUsersEmail(string projectDir)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return "";
            }
            return SoftwareCoUtil.GetFirstCommandResult("git config user.email", projectDir);
        }

        public static string GetRepoUrlLink(string projectDir)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return "";
            }
            string repoUrl = SoftwareCoUtil.GetFirstCommandResult("git config --get remote.origin.url", projectDir);
            if (repoUrl != null)
            {
                repoUrl = repoUrl.Substring(0, repoUrl.LastIndexOf(".git"));
            }
            return repoUrl;
        }

        public static CommitInfo GetLastCommitInfo(string projectDir, string email)
        {
            if (!SoftwareCoUtil.IsGitProject(projectDir))
            {
                return new CommitInfo();
            }
            CommitInfo commitInfo = new CommitInfo();

            string authorArg = (email != null) ? " --author=" + email + " " : " ";
            string cmd = "git log --pretty=%H,%s" + authorArg + "--max-count=1";

            List<string> results = SoftwareCoUtil.GetCommandResultList(cmd, projectDir);

            if (results != null && results.Count > 0)
            {
                string[] parts = results[0].Split(',');
                if (parts != null && parts.Length == 2)
                {
                    commitInfo.commitId = parts[0];
                    commitInfo.comment = parts[1];
                    commitInfo.email = email;
                }
            }

            return commitInfo;
        }

        public static async Task<RepoCommit> GetLatestCommitAsync(string projectDir)
        {
            try
            {
                if (!SoftwareCoUtil.IsGitProject(projectDir))
                {
                    return null;
                }
                RepoResourceInfo info = GetResourceInfo(projectDir, false);

                if (info != null && info.identifier != null)
                {
                    string identifier = info.identifier;
                    if (identifier != null && !identifier.Equals(""))
                    {
                        string tag = info.tag;
                        string branch = info.branch;

                        string qryString = "?identifier=" + identifier;
                        qryString += "&tag=" + tag;
                        qryString += "&branch=" + branch;

                        HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                                HttpMethod.Get, "/commits/latest?" + qryString);

                        if (SoftwareHttpManager.IsOk(response))
                        {

                            // get the json data
                            string responseBody = await response.Content.ReadAsStringAsync();
                            IDictionary<string, object> jsonObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);

                            jsonObj.TryGetValue("commitId", out object commitIdObj);
                            string commitId = (commitIdObj == null) ? "" : Convert.ToString(commitIdObj);

                            jsonObj.TryGetValue("message", out object messageObj);
                            string message = (messageObj == null) ? "" : Convert.ToString(messageObj);

                            jsonObj.TryGetValue("message", out object timestampObj);
                            long timestamp = (timestampObj == null) ? 0L : Convert.ToInt64(timestampObj);

                            RepoCommit repoCommit = new RepoCommit(commitId, message, timestamp);
                            return repoCommit;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetLatestCommitAsync error: " + ex.Message, ex);

            }
            return null;
        }

        public static async void GetHistoricalCommitsAsync(string projectDir)
        {
            try
            {
                if (!SoftwareCoUtil.IsGitProject(projectDir))
                {
                    return;
                }
                RepoResourceInfo info = GetResourceInfo(projectDir, false);

                if (info != null && info.identifier != null)
                {
                    string identifier = info.identifier;
                    if (identifier != null && !identifier.Equals(""))
                    {
                        string tag = info.tag;
                        string branch = info.branch;
                        string email = info.email;

                        RepoCommit latestCommit = null;
                        latestCommit = await GetLatestCommitAsync(projectDir);

                        string sinceOption = "";
                        if (latestCommit != null)
                        {
                            sinceOption = " --since=" + latestCommit.timestamp;
                        }
                        else
                        {
                            sinceOption = " --max-count=100";
                        }

                        string cmd = "git log --stat --pretty=COMMIT:%H,%ct,%cI,%s --author=" + email + "" + sinceOption;

                        string gitCommitData = SoftwareCoUtil.GetFirstCommandResult(cmd, projectDir);

                        if (gitCommitData != null && !gitCommitData.Equals(""))
                        {
                            string[] lines = gitCommitData.Split(
                                new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                            RepoCommit currentRepoCommit = null;
                            List<RepoCommit> repoCommits = new List<RepoCommit>();
                            if (lines != null && lines.Length > 0)
                            {
                                for (int i = 0; i < lines.Length; i++)
                                {
                                    string line = lines[i].Trim();
                                    if (line.Length > 0)
                                    {
                                        bool hasPipe = line.IndexOf("|") != -1 ? true : false;
                                        bool isBin = line.ToLower().IndexOf("bin") != -1 ? true : false;
                                        if (line.IndexOf("COMMIT:") == 0)
                                        {
                                            line = line.Substring("COMMIT:".Length);
                                            if (currentRepoCommit != null)
                                            {
                                                repoCommits.Add(currentRepoCommit);
                                            }

                                            string[] commitInfos = line.Split(',');
                                            if (commitInfos != null && commitInfos.Length > 0)
                                            {
                                                string commitId = commitInfos[0].Trim();
                                                // go to the next line if we've already processed this commitId
                                                if (latestCommit != null && commitId.Equals(latestCommit.commitId))
                                                {
                                                    currentRepoCommit = null;
                                                    continue;
                                                }

                                                // get the other attributes now
                                                long timestamp = Convert.ToInt64(commitInfos[1].Trim());
                                                string date = commitInfos[2].Trim();
                                                string message = commitInfos[3].Trim();
                                                currentRepoCommit = new RepoCommit(commitId, message, timestamp);
                                                currentRepoCommit.date = date;

                                                RepoCommitChanges changesObj = new RepoCommitChanges(0, 0);
                                                currentRepoCommit.changes.Add("__sftwTotal__", changesObj);
                                            }
                                        }
                                        else if (currentRepoCommit != null && hasPipe && !isBin)
                                        {
                                            // get the file and changes
                                            // i.e. somefile.cs                             | 20 +++++++++---------
                                            line = string.Join(" ", line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                                            string[] lineInfos = line.Split('|');
                                            if (lineInfos != null && lineInfos.Length > 1)
                                            {
                                                string file = lineInfos[0].Trim();
                                                string[] metricInfos = lineInfos[1].Trim().Split(' ');
                                                if (metricInfos != null && metricInfos.Length > 1)
                                                {
                                                    string addAndDeletes = metricInfos[1].Trim();
                                                    int len = addAndDeletes.Length;
                                                    int lastPlusIdx = addAndDeletes.LastIndexOf('+');
                                                    int insertions = 0;
                                                    int deletions = 0;
                                                    if (lastPlusIdx != -1)
                                                    {
                                                        insertions = lastPlusIdx + 1;
                                                        deletions = len - insertions;
                                                    }
                                                    else if (len > 0)
                                                    {
                                                        // all deletions
                                                        deletions = len;
                                                    }

                                                    if (!currentRepoCommit.changes.ContainsKey(file))
                                                    {
                                                        RepoCommitChanges changesObj = new RepoCommitChanges(insertions, deletions);
                                                        currentRepoCommit.changes.Add(file, changesObj);
                                                    }
                                                    else
                                                    {
                                                        RepoCommitChanges fileCommitChanges;
                                                        currentRepoCommit.changes.TryGetValue(file, out fileCommitChanges);
                                                        if (fileCommitChanges != null)
                                                        {
                                                            fileCommitChanges.deletions += deletions;
                                                            fileCommitChanges.insertions += insertions;
                                                        }
                                                    }


                                                    RepoCommitChanges totalRepoCommit;
                                                    currentRepoCommit.changes.TryGetValue("__sftwTotal__", out totalRepoCommit);
                                                    if (totalRepoCommit != null)
                                                    {
                                                        totalRepoCommit.deletions += deletions;
                                                        totalRepoCommit.insertions += insertions;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (currentRepoCommit != null)
                            {
                                repoCommits.Add(currentRepoCommit);
                            }

                            if (repoCommits != null && repoCommits.Count > 0)
                            {
                                // batch 10 at a time
                                int batch_size = 10;
                                List<RepoCommit> batch = new List<RepoCommit>();
                                for (int i = 0; i < repoCommits.Count; i++)
                                {
                                    batch.Add(repoCommits[i]);
                                    if (i > 0 && i % batch_size == 0)
                                    {
                                        // send this batch.
                                        RepoCommitData commitData = new RepoCommitData(identifier, tag, branch, batch);

                                        string jsonContent = JsonConvert.SerializeObject(commitData);

                                        HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                                            HttpMethod.Post, "/commits", jsonContent);

                                        if (SoftwareHttpManager.IsOk(response))
                                        {
                                            Logger.Info(response.ToString());
                                        }
                                        else
                                        {
                                            Logger.Error(response.ToString());
                                        }
                                    }
                                }

                                if (batch.Count > 0)
                                {
                                    RepoCommitData commitData = new RepoCommitData(identifier, tag, branch, batch);

                                    string jsonContent = JsonConvert.SerializeObject(commitData);

                                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                                        HttpMethod.Post, "/commits", jsonContent);

                                    if (SoftwareHttpManager.IsOk(response))
                                    {
                                        Logger.Info(response.ToString());
                                    }
                                    else if (response != null)
                                    {
                                        Logger.Error("Unable to complete commit request, status: " + response.StatusCode);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetHistoricalCommitsAsync ,error: " + ex.Message, ex);

            }

        }
    }
}
