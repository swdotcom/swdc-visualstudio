
using System.Collections.Generic;

namespace SoftwareCo
{
    class RepoCommitData
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
    }
}
