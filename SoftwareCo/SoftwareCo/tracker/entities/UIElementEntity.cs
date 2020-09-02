using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    class UIElementEntity
    {
        public string element_name { get; set; }
        public string element_location { get; set; }
        public string color { get; set; }
        public string icon_name { get; set; }
        public string cta_text { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/ui_element/jsonschema/1-0-3")
                .Add("element_name", element_name)
                .Add("element_location", element_location)
                .Add("color", color)
                .Add("icon_name", icon_name)
                .Add("cta_text", cta_text)
                .Build();
            return context;
        }
    }
}
