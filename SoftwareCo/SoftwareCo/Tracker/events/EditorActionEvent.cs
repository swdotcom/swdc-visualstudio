using System.Collections.Generic;
using Snowplow.Tracker.Models.Contexts;
using Snowplow.Tracker.Models.Events;

namespace SoftwareCo
{
    public class EditorActionEvent
    {
        public string entity { get; set; }
        public string type { get; set; }

        public AuthEntity authEntity { get; set; }
        public FileEntity fileEntity { get; set; }
        public PluginEntity pluginEntity { get; set; }
        public ProjectEntity projectEntity { get; set; }
        public RepoEntity repoEntity { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/editor_action/jsonschema/1-0-1")
                .Add("entity", entity)
                .Add("type", type)
                .Build();
            return context;
        }

        public SelfDescribing buildContexts()
        {
            List<IContext> contexts = new List<IContext>();
            GenericContext eventContext = buildContext();
            contexts.Add(eventContext);

            contexts.Add(authEntity.buildContext());
            contexts.Add(fileEntity.buildContext());
            contexts.Add(pluginEntity.buildContext());
            contexts.Add(projectEntity.buildContext());
            contexts.Add(repoEntity.buildContext());

            return new SelfDescribing().SetEventData(eventContext.GetJson()).SetCustomContext(contexts).Build();
        }
    }
}
