using System;

namespace SoftwareCo
{
    public class CommitInfo
    {
        public string email { get; set; }
        public string commitId { get; set; }
        public string comment { get; set; }
        public Int32 insertions { get; set; }
        public Int32 deletions { get; set; }
        public Int32 fileCount { get; set; }
        public Int32 commitCount { get; set; }
    }
}
