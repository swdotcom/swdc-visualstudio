
namespace SoftwareCo
{
    class RepoCommitChanges
    {
        public int insertions = 0;
        public int deletions = 0;
        public RepoCommitChanges(int insertions, int deletions)
        {
            this.insertions = insertions;
            this.deletions = deletions;
        }
    }
}
