using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    public class RepoEntity
    {
        public string repo_identifier { get; set; }
        public string repo_name { get; set; }
        public string owner_id { get; set; }
        public string git_branch { get; set; }
        public string git_tag { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/repo/jsonschema/1-0-0")
                .Add("repo_identifier", HashManager.HashValue(repo_identifier, "repo_identifier"))
                .Add("repo_name", HashManager.HashValue(repo_name, "repo_name"))
                .Add("git_branch", HashManager.HashValue(git_branch, "git_branch"))
                .Add("git_tag", HashManager.HashValue(git_tag, "git_tag"))
                .Build();
            return context;
        }
    }
}
