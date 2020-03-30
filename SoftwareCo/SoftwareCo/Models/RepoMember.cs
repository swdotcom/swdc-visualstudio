using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class RepoMember
    {
        public string name = "";
        public string email = "";
        public string identifier = "";
        public RepoMember(string name, string email)
        {
            this.name = name;
            this.email = email;
        }
    }
}
