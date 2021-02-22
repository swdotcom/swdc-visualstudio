using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    class SoftwareUser
    {
        public long id { get; set; }
        public int registered { get; set; }
        public String email { get; set; }
        public List<Integration> integrations { get; set; }
        public String plugin_jwt { get; set; }
    }
}
