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
        public String identifier;

        public PluginDataProject(String nameVal, String directoryVal)
        {
            name = nameVal;
            directory = directoryVal;
            // get the identifier
            RepoResourceInfo resourceInfo = SoftwareCoUtil.GetResourceInfo(directoryVal);
            if (resourceInfo != null && resourceInfo.identifier != null)
            {
                identifier = resourceInfo.identifier;
            }
        }

        public static PluginDataProject GetPluginDataFromDictionary(IDictionary<string, object> dict)
        {
            string projName = SoftwareCoUtil.ConvertObjectToString(dict, "name");
            string projDir = SoftwareCoUtil.ConvertObjectToString(dict, "directory");
            string identifierVal = SoftwareCoUtil.ConvertObjectToString(dict, "identifier");
            PluginDataProject project = new PluginDataProject(projName, projDir);
            project.identifier = identifierVal;
            return project;
        }

        public IDictionary<string, object> GetAsDictionary()
        {
            IDictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("name", this.name);
            dict.Add("directory", this.directory);
            dict.Add("identifier", this.identifier);
            return dict;
        }

        public JsonObject GetAsJson()
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("name", this.name);
            jsonObj.Add("directory", this.directory);
            jsonObj.Add("identifier", this.identifier);
            return jsonObj;
        }
    }
}
