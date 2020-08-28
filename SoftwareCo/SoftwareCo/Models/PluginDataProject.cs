using System.Collections.Generic;

namespace SoftwareCo
{
    public class PluginDataProject
    {
        public string name;
        public string directory;
        public string identifier;

        public PluginDataProject(string nameVal, string directoryVal)
        {
            name = nameVal;
            directory = directoryVal;
            // get the identifier
            RepoResourceInfo resourceInfo = GitUtilManager.GetResourceInfo(directoryVal, false);
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
