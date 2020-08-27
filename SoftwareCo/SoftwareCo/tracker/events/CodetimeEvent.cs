using Snowplow.Tracker.Models.Contexts;
using Snowplow.Tracker.Models.Events;
using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    class CodetimeEvent
    {
        public long keystrokes { get; set; }
        public long chars_added { get; set; }
        public long chars_deleted { get; set; }
        public long chars_pasted { get; set; }
        public long pastes { get; set; }
        public long lines_added { get; set; }
        public long lines_deleted { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }
        public int single_deletes { get; set; }
        public int multi_deletes { get; set; }
        public int single_adds { get; set; }
        public int multi_adds { get; set; }
        public int auto_indents { get; set; }
        public int replacements { get; set; }
        public bool is_net_change { get; set; }

        public AuthEntity authEntity { get; set; }
        public FileEntity fileEntity { get; set; }
        public PluginEntity pluginEntity { get; set; }
        public ProjectEntity projectEntity { get; set; }
        public RepoEntity repoEntity { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/codetime/jsonschema/1-0-2")
                .Add("keystrokes", keystrokes)
                .Add("characters_added", chars_added)
                .Add("characters_deleted", chars_deleted)
                .Add("lines_added", lines_added)
                .Add("lines_deleted", lines_deleted)
                .Add("start_time", start_time)
                .Add("end_time", end_time)
                .Add("single_deletes", single_deletes)
                .Add("multi_deletes", multi_deletes)
                .Add("single_adds", single_adds)
                .Add("multi_adds", multi_adds)
                .Add("auto_indents", auto_indents)
                .Add("replacements", replacements)
                .Add("is_net_change", is_net_change)
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
