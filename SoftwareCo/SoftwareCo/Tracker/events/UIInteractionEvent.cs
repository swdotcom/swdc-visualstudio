using System.Collections.Generic;
using Snowplow.Tracker.Models.Contexts;
using Snowplow.Tracker.Models.Events;

namespace SwdcVsTracker
{
    public class UIInteractionEvent
    {
        public UIInteractionType interaction_type { get; set; }

        public AuthEntity authEntity { get; set; }
        public PluginEntity pluginEntity { get; set; }
        public UIElementEntity uiElementEntity { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/editor_action/jsonschema/1-0-1")
                .Add("interaction_type", interaction_type.ToString())
                .Build();
            return context;
        }

        public SelfDescribing buildContexts()
        {
            List<IContext> contexts = new List<IContext>();
            GenericContext eventContext = buildContext();
            contexts.Add(eventContext);

            contexts.Add(authEntity.buildContext());
            contexts.Add(pluginEntity.buildContext());
            contexts.Add(uiElementEntity.buildContext());

            return new SelfDescribing().SetEventData(eventContext.GetJson()).SetCustomContext(contexts).Build();
        }
    }
}
