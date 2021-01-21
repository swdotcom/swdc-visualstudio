using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SoftwareCo
{
    public class Integration
    {
        public long id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
        public string status { get; set; }
        public string authId { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public List<string> scopes { get; set; }
        public string plugin_uuid { get; set; }
        public string team_domain { get; set; }
        public string team_name { get; set; }
        public string integration_id { get; set; }
    }
}
