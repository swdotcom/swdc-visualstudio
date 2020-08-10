using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    public class PluginEntity
    {
        public int plugin_id { get; set; }
        public string plugin_version { get; set; }
        public string plugin_name { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/plugin/jsonschema/1-0-1")
                .Add("plugin_id", plugin_id)
                .Add("plugin_version", plugin_version)
                .Add("plugin_name", plugin_name)
                .Build();
            return context;
        }
    }
}
