﻿using System.Collections.Generic;
using Snowplow.Tracker.Models.Contexts;
using Snowplow.Tracker.Models.Events;

namespace SwdcVsTracker
{
    public class CodetimeEvent
    {
        public int keystrokes { get; set; }
        public int chars_added { get; set; }
        public int chars_deleted { get; set; }
        public int chars_pasted { get; set; }
        public int pastes { get; set; }
        public int lines_added { get; set; }
        public int lines_deleted { get; set; }
        public string start_time { get; set; }
        public string end_time { get; set; }

        public AuthEntity authEntity { get; set; }
        public FileEntity fileEntity { get; set; }
        public PluginEntity pluginEntity { get; set; }
        public ProjectEntity projectEntity { get; set; }
        public RepoEntity repoEntity { get; set; }

        public GenericContext buildContext()
        {
            GenericContext context = new GenericContext()
                .SetSchema("iglu:com.software/codetime/jsonschema/1-0-1")
                .Add("keystrokes", keystrokes)
                .Add("keystrokes", keystrokes)
                .Add("chars_added", chars_added)
                .Add("chars_deleted", chars_deleted)
                .Add("chars_pasted", chars_pasted)
                .Add("pastes", pastes)
                .Add("lines_added", lines_added)
                .Add("lines_deleted", lines_deleted)
                .Add("start_time", start_time)
                .Add("end_time", end_time)
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
