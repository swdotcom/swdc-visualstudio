using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

using Snowplow.Tracker.Emitters;
using Snowplow.Tracker.Endpoints;
using Snowplow.Tracker.Models;
using Snowplow.Tracker.Models.Events;
using Snowplow.Tracker.Models.Adapters;
using Snowplow.Tracker.Queues;
using Snowplow.Tracker.Storage;
using Snowplow.Tracker;

namespace SoftwareCo
{
    public class TrackerManager
    {
        private string swdcApiHost = "";
        private string trackerNamespace = "";
        private string appId = "";
        private Tracker t = null;

        public TrackerManager(string swdcApiHost, string trackerNamespace, string appId)
        {
            this.swdcApiHost = swdcApiHost;
            this.trackerNamespace = trackerNamespace;
            this.appId = appId;

            // _ is a discard variable since the initialize tracker is async
            _ = initializeTracker();
        }

        private async Task initializeTracker()
        {
            // initialie our http client with the endpoint that fetches the snowplow collector endpoint
            Http.Initialize(swdcApiHost);

            // fetch the tracker_api from the plugin config
            Response resp = await Http.GetAsync("/plugins/config");

            if (resp.ok && resp.responseData != null)
            {
                string json = JsonConvert.SerializeObject(resp.responseData);
                Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                string track_api_host = SoftwareCoUtil.TryGetStringFromDictionary(dictionary, "tracker_api");

                // Controls the sending of events
                SnowplowHttpCollectorEndpoint endpoint = new SnowplowHttpCollectorEndpoint(track_api_host, HttpProtocol.HTTPS, null, HttpMethod.POST);

                // Controls the storage of events
                // NOTE: You must dispose of storage yourself when closing your application!
                LiteDBStorage storage = new LiteDBStorage("events.db");

                // Controls queueing events
                PersistentBlockingQueue queue = new PersistentBlockingQueue(storage, new PayloadToJsonString());

                // Controls pulling events of the queue and pushing them to the sender
                AsyncEmitter emitter = new AsyncEmitter(endpoint, queue);
                Subject subject = new Subject().SetPlatform(Platform.Iot).SetLang("EN");

                t = Tracker.Instance;

                t.Start(emitter, subject, null, trackerNamespace, appId, false /*encodeBase64*/, false /*synchronous*/);
            }
        }

        public void TrackCodetimeEvent(CodetimeEvent codetimeEvent)
        {
            CacheManager.jwt = codetimeEvent.authEntity != null ? codetimeEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = codetimeEvent.buildContexts();
        }

        public void TrackEditorActionEvent(EditorActionEvent editorActionEvent)
        {
            CacheManager.jwt = editorActionEvent.authEntity != null ? editorActionEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = editorActionEvent.buildContexts();
        }

        public void TrackUIInteractionEvent(UIInteractionEvent uIInteractionEvent)
        {
            CacheManager.jwt = uIInteractionEvent.authEntity != null ? uIInteractionEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = uIInteractionEvent.buildContexts();
        }

        private void sendEvent(SelfDescribing selfDescribing)
        {
            if (selfDescribing != null)
            {
                t.Track(selfDescribing);
            }
        }

    }
}
