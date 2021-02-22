using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class PluginStateInfo
    {
        public string state { get; set; } // "OK"
        public string jwt { get; set; }
        public string email { get; set; }
        public SoftwareUser user { get; set; }
    }
}
