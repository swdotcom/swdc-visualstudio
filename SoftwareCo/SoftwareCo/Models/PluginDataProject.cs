using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public class PluginDataProject
    {
        public String name;
        public String directory;

        public PluginDataProject(String nameVal, String directoryVal)
        {
            name = nameVal;
            directory = directoryVal;
        }

        public IDictionary<string, object> GetAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("name", this.name);
            dict.Add("directory", this.directory);
            return dict;
        }

        public JsonObject GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("name", this.name);
            jsonObj.Add("directory", this.directory);
            return jsonObj;
        }
    }
}
