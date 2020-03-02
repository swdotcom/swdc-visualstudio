using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class CodeTimeEvent
    {
        public string type { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string os { get; set; }
        public string version { get; set; }
        public string hostname { get; set; }
        public string timezone { get; set; }
        public long timestamp { get; set; }
        public long timestamp_local { get; set; }
        public Int32 pluginId { get; set; }

        public CodeTimeEvent()
        {
            //
        }

        public CodeTimeEvent(string typeVal, string nameVal, string descriptionVal)
        {
            this.type = typeVal;
            this.name = nameVal;
            this.description = descriptionVal;
            this.InitializeData();
        }

        private void InitializeData() {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            this.timestamp = nowTime.now;
            this.timestamp_local = nowTime.local_now;
            this.pluginId = Constants.PluginId;
            this.os = SoftwareCoPackage.GetOs();
            this.hostname = SoftwareCoUtil.getHostname();
            this.version = SoftwareCoPackage.GetVersion();
            if (TimeZone.CurrentTimeZone.DaylightName != null
                && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
            {
                this.timezone = TimeZone.CurrentTimeZone.DaylightName;
            }
            else
            {
                this.timezone = TimeZone.CurrentTimeZone.StandardName;
            }
        }

        public void CloneFromDictionary(IDictionary<string, object> dict)
        {
            this.timestamp = SoftwareCoUtil.ConvertObjectToLong(dict, "timestamp");
            this.timestamp_local = SoftwareCoUtil.ConvertObjectToLong(dict, "timestamp_local");
            this.type = SoftwareCoUtil.ConvertObjectToString(dict, "type");
            this.name = SoftwareCoUtil.ConvertObjectToString(dict, "name");
            this.description = SoftwareCoUtil.ConvertObjectToString(dict, "description");
            this.os = SoftwareCoUtil.ConvertObjectToString(dict, "os");
            this.version = SoftwareCoUtil.ConvertObjectToString(dict, "version");
            this.timezone = SoftwareCoUtil.ConvertObjectToString(dict, "timezone");
            this.hostname = SoftwareCoUtil.ConvertObjectToString(dict, "hostname");
            this.pluginId = SoftwareCoUtil.ConvertObjectToInt(dict, "pluginId");
        }

        public JsonObject GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("timestamp", this.timestamp);
            jsonObj.Add("timestamp_local", this.timestamp_local);
            jsonObj.Add("type", this.type);
            jsonObj.Add("name", this.name);
            jsonObj.Add("description", this.description);
            jsonObj.Add("os", this.os);
            jsonObj.Add("version", this.version);
            jsonObj.Add("hostname", this.hostname);
            jsonObj.Add("timezone", this.timezone);
            jsonObj.Add("pluginId", this.pluginId);
            return jsonObj;
        }

        public string GetAsJsonString()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("timestamp", this.timestamp);
            jsonObj.Add("timestamp_local", this.timestamp_local);
            jsonObj.Add("type", this.type);
            jsonObj.Add("name", this.name);
            jsonObj.Add("description", this.description);
            jsonObj.Add("os", this.os);
            jsonObj.Add("version", this.version);
            jsonObj.Add("hostname", this.hostname);
            jsonObj.Add("timezone", this.timezone);
            jsonObj.Add("pluginId", this.pluginId);
            return jsonObj.ToString();
        }
    }
}
