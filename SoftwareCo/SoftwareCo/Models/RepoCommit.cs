using System.Collections.Generic;

namespace SoftwareCo
{
    class RepoCommit
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
    }
}
