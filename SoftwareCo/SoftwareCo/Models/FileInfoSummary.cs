using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class FileInfoSummary
    {
        public long add { get; set; }
        public long close { get; set; }
        public long delete { get; set; }
        public long linesAdded { get; set; }
        public long linesRemoved { get; set; }
        public long open { get; set; }
        public long paste { get; set; }
        public long keystrokes { get; set; }
        public long netkeys { get; set; }
        public string syntax { get; set; }
        public long start { get; set; }
        public long end { get; set; }
        public long local_start { get; set; }
        public long local_end { get; set; }
        public long duration_seconds { get; set; }
        public string fsPath { get; set; }
        public string name { get; set; }
    }
}
