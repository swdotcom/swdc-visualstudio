using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    public class FileEntity
    {
        public string file_name { get; set; }
        public string file_path { get; set; }
        public string syntax { get; set; }
        public int line_count { get; set; }
        public long character_count { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/file/jsonschema/1-0-1")
                .Add("file_name", HashManager.HashValue(this.file_name, "file_name"))
                .Add("file_path", HashManager.HashValue(this.file_name, "file_path"))
                .Add("syntax", syntax)
                .Add("line_count", line_count)
                .Add("character_count", character_count)
                .Build();
            return context;
        }
    }
}
