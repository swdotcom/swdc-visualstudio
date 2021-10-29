using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Net.Http;

namespace SoftwareCo
{
    public sealed class FlowManager
    {
        private static readonly Lazy<FlowManager> lazy = new Lazy<FlowManager>(() => new FlowManager());
        public static FlowManager Instance { get { return lazy.Value; } }

        private FlowManager()
        {
        }

        public async void init()
        {
            HttpResponseMessage response = await SoftwareHttpManager.MetricsRequest(HttpMethod.Get, "/v1/flow_sessions", null);
            if (SoftwareHttpManager.IsOk(response))
            {
                try
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    string cleanJson = SoftwareCoUtil.CleanJsonToDeserialize(responseBody);
                    JsonArray flowSessions = JsonConvert.DeserializeObject<JsonArray>(cleanJson);
                    bool inFlow = (flowSessions != null && flowSessions.Count > 0);
                    FileManager.UpdateFlowChange(inFlow);
                }
                catch (Exception e)
                {
                    Logger.Warning("Error retrieving flow sessions: " + e.Message);
                }
            }
        }

        public async void EnableFlow(bool automated)
        {
            if (!FileManager.IsInFlow())
            {
                JsonObject jsonObj = new JsonObject();
                jsonObj.Add("automated", automated);
                await SoftwareHttpManager.MetricsRequest(HttpMethod.Post, "/v1/flow_sessions", jsonObj.ToString());
                FileManager.UpdateFlowChange(true);
            }

            PackageManager.RebuildTreeAsync();
        }

        public async void DisableFlow()
        {
            if (FileManager.IsInFlow())
            {
                await SoftwareHttpManager.MetricsRequest(HttpMethod.Delete, "/v1/flow_sessions", null);
                FileManager.UpdateFlowChange(false);
            }

            PackageManager.RebuildTreeAsync();
        }

    }
}
