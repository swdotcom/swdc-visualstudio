using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class CommitChangeStats
    {
        public long insertions { get; set; }
        public long deletions { get; set; }
        public long fileCount { get; set; }
        public long commitCount { get; set; }
    }
}
