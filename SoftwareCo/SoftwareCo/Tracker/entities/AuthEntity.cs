using Snowplow.Tracker.Models.Contexts;

namespace SoftwareCo
{
    public class AuthEntity
    {
        public string jwt { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/auth/jsonschema/1-0-0")
                .Add("jwt", jwt)
                .Build();
            return context;
        }

    }
}
