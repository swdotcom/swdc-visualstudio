using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class FileDetails
    {
        public string full_file_name { get; set; } // the absolute path and file name
        public string file_name { get; set; } // the base name
        public string project_file_name { get; set; } // the path from the project directory including the file name
        public string project_name { get; set; } // the base project name
        public string project_directory { get; set; } // the project path including the project name
        public string syntax { get; set; }
        public int line_count { get; set; }
        public long character_count { get; set; }
    }
}
