

using Newtonsoft.Json;
using Snowplow.Tracker;
using Snowplow.Tracker.Emitters;
using Snowplow.Tracker.Endpoints;
using Snowplow.Tracker.Models;
using Snowplow.Tracker.Models.Adapters;
using Snowplow.Tracker.Models.Events;
using Snowplow.Tracker.Queues;
using Snowplow.Tracker.Storage;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class TrackerManager
    {
        private string trackerNamespace = "";
        private string appId = "";
        private Tracker t = null;
        private static LiteDBStorage storage = null;

        public bool initialized = false;

        public TrackerManager(string trackerNamespace, string appId)
        {
            this.trackerNamespace = trackerNamespace;
            this.appId = appId;
        }

        public async Task initializeTracker()
        {
            if (!initialized)
            {

                // fetch the tracker_api from the plugin config
                HttpResponseMessage resp = await SoftwareHttpManager.SendRequestAsync(System.Net.Http.HttpMethod.Get, "/plugins/config", null, null, false);

                // if (resp.ok && resp.responseData != null)
                if (SoftwareHttpManager.IsOk(resp))
                {
                    // get the json data
                    string json = await resp.Content.ReadAsStringAsync();
                    // string json = JsonConvert.SerializeObject(resp.responseData);
                    Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                    string track_api_host = DictionaryUtil.TryGetStringFromDictionary(dictionary, "tracker_api");

                    // Controls the sending of events
                    SnowplowHttpCollectorEndpoint endpoint = new SnowplowHttpCollectorEndpoint(track_api_host, HttpProtocol.HTTPS, null, Snowplow.Tracker.Endpoints.HttpMethod.POST);

                    // Controls the storage of events
                    // NOTE: You must dispose of storage yourself when closing your application!
                    storage = new LiteDBStorage(FileManager.GetSnowplowStorageFile());

                    // Controls queueing events
                    PersistentBlockingQueue queue = new PersistentBlockingQueue(storage, new PayloadToJsonString());

                    // Controls pulling events of the queue and pushing them to the sender
                    AsyncEmitter emitter = new AsyncEmitter(endpoint, queue);
                    Subject subject = new Subject().SetPlatform(Platform.Iot).SetLang("EN");

                    t = Tracker.Instance;
                    if (!t.Started)
                    {
                        t.Start(emitter, subject, null, trackerNamespace, appId, false /*encodeBase64*/, false /*synchronous*/);
                    }
                    initialized = true;
                }
            }
        }

        public static void Dispose()
        {
            if (storage != null)
            {
                try
                {
                    storage.Dispose();
                }
                catch (Exception) { }
            }
        }

        public void TrackCodetimeEvent(CodetimeEvent codetimeEvent)
        {
            if (t == null)
            {
                return;
            }
            CacheManager.jwt = codetimeEvent.authEntity != null ? codetimeEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = codetimeEvent.buildContexts();
            t.Track(selfDescribing);
        }

        public void TrackEditorActionEvent(EditorActionEvent editorActionEvent)
        {
            if (t == null)
            {
                return;
            }
            CacheManager.jwt = editorActionEvent.authEntity != null ? editorActionEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = editorActionEvent.buildContexts();
            t.Track(selfDescribing);
        }

        public void TrackUIInteractionEvent(UIInteractionEvent uIInteractionEvent)
        {
            if (t == null)
            {
                return;
            }
            CacheManager.jwt = uIInteractionEvent.authEntity != null ? uIInteractionEvent.authEntity.jwt : "";
            SelfDescribing selfDescribing = uIInteractionEvent.buildContexts();
            t.Track(selfDescribing);
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
