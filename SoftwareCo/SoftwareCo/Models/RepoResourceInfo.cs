
using System.Collections.Generic;

namespace SoftwareCo
{
    public class RepoResourceInfo
    {
        public string email { get; set; }
        public string tag { get; set; }
        public string branch { get; set; }
        public string identifier { get; set; }
        public string repoName { get; set; }
        public string ownerId { get; set; }
        public List<RepoMember> members { get; set; }

        public RepoResourceInfo() {
            members = new List<RepoMember>();
        }

        public IDictionary<string, string> GetAsDictionary()
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();

            dict.Add("identifier", identifier);

            dict.Add("email", email);

            dict.Add("branch", branch);

            dict.Add("tag", tag);
            return dict;
        }
    }
}
