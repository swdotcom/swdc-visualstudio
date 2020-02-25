using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class RepoResourceInfo
    {
        public string email { get; set; }
        public string tag { get; set; }
        public string branch { get; set; }
        public string identifier { get; set; }

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
