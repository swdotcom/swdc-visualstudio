
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class SoftwareRepoManager
    {

        public class RepoCommitChanges
        {
            public int insertions = 0;
            public int deletions = 0;
            public RepoCommitChanges(int insertions, int deletions)
            {
                this.insertions = insertions;
                this.deletions = deletions;
            }
        }

        public class RepoCommit
        {
            public string commitId = "";
            public string message = "";
            public long timestamp = 0L;
            public string date = "";
            public IDictionary<string, RepoCommitChanges> changes = new Dictionary<string, RepoCommitChanges>();

            public RepoCommit(string commitId, string message, long timestamp)
            {
                this.commitId = commitId;
                this.message = message;
                this.timestamp = timestamp;
            }

            public JsonObject GetAsJsonObj()
            {
                JsonObject jsonObj = new JsonObject();
                jsonObj.Add("commitId", this.commitId);
                jsonObj.Add("message", this.message);
                jsonObj.Add("timestamp", this.timestamp);

                JsonObject changesJsonObj = new JsonObject();
                foreach (string key in changes.Keys)
                {
                    RepoCommitChanges commitChanges = changes[key];
                    JsonObject changesObj = new JsonObject();
                    changesObj.Add("deletions", commitChanges.deletions);
                    changesObj.Add("insertions", commitChanges.insertions);
                    changesJsonObj.Add(key, changesObj);
                }
                jsonObj.Add("changes", changesJsonObj);
                return jsonObj;
            }
        }

        public class RepoData
        {
            public string identifier = "";
            public string tag = "";
            public string branch = "";
            public List<RepoMember> members = new List<RepoMember>();
            public RepoData(string identifier, string tag, string branch, List<RepoMember> members)
            {
                this.identifier = identifier;
                this.tag = tag;
                this.branch = branch;
                this.members = members;
            }
        }

        public class RepoCommitData
        {
            public string identifier = "";
            public string tag = "";
            public string branch = "";
            public List<RepoCommit> commits = new List<RepoCommit>();
            public RepoCommitData(string identifier, string tag, string branch, List<RepoCommit> commits)
            {
                this.identifier = identifier;
                this.tag = tag;
                this.branch = branch;
                this.commits = commits;
            }

            public string GetAsJson()
            {
                JsonObject jsonObj = new JsonObject();
                jsonObj.Add("identifier", this.identifier);
                jsonObj.Add("tag", this.tag);
                jsonObj.Add("branch", this.branch);
                JsonArray jsonArr = new JsonArray();
                foreach (RepoCommit commit in commits)
                {
                    jsonArr.Add(commit.GetAsJsonObj());
                }
                jsonObj.Add("commits", jsonArr);
                return jsonObj.ToString();
            }
        }
    }
}
