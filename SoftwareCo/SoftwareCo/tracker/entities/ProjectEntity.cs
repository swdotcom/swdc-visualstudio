using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    class ProjectEntity
    {
        public string project_name { get; set; }
        public string project_directory { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/project/jsonschema/1-0-0")
                .Add("project_name", HashManager.HashValue(this.project_name, "project_name"))
                .Add("project_directory", HashManager.HashValue(this.project_name, "project_directory"))
                .Build();
            return context;
        }
    }
}
