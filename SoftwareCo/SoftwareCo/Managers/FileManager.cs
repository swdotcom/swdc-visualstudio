using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SoftwareCo
{
    class FileManager
    {

        public static string getDashboardFile()
        {
            return getSoftwareDataDir(true) + "\\CodeTime.txt";
        }

        public static string GetContributorDashboardFile()
        {
            return getSoftwareDataDir(true) + "\\ProjectContributorCodeSummary.txt";
        }

        public static string getVSReadmeFile()
        {
            return getSoftwareDataDir(true) + "\\VS_README.txt";
        }

        public static string getSoftwareDataDir(bool autoCreate)
        {
            string userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (autoCreate && !Directory.Exists(softwareDataDir))
            {
                try
                {
                    // create it
                    Directory.CreateDirectory(softwareDataDir);
                }
                catch (Exception)
                { }
            }
            return softwareDataDir;
        }

        public static string GetSnowplowStorageFile()
        {
            string file = getSoftwareDataDir(true) + "\\events.db";
            return file;
        }

        public static bool softwareSessionFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\session.json";
            return File.Exists(file);
        }

        public static bool SessionSummaryFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\sessionSummary.json";
            return File.Exists(file);
        }
        public static string getSessionSummaryFile()
        {
            return getSoftwareDataDir(true) + "\\sessionSummary.json";
        }

        private static String getIntegrationsFile()
        {
            return getSoftwareDataDir(true) + "\\integrations.json";
        }

        public static string getSessionSummaryFileData()
        {
            try
            {
                return File.ReadAllText(getSoftwareDataDir(true) + "\\sessionSummary.json", System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
                return new JsonObject().ToString();
            }
        }

        public static string getSoftwareSessionFile()
        {
            return getSoftwareDataDir(true) + "\\session.json";
        }

        public static string getDeviceFile()
        {
            return getSoftwareDataDir(true) + "\\device.json";
        }

        public static bool FileChangeInfoSummaryFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\fileChangeSummary.json";
            return File.Exists(file);
        }
        public static string getFileChangeInfoSummaryData()
        {
            try
            {
                return File.ReadAllText(getSoftwareDataDir(true) + "\\fileChangeSummary.json", System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
                return new JsonObject().ToString();
            }
        }

        public static string getFileChangeInfoSummaryFile()
        {
            return getSoftwareDataDir(true) + "\\fileChangeSummary.json";
        }

        public static string getSessionSummaryInfoFile()
        {
            return getSoftwareDataDir(true) + "\\SummaryInfo.txt";
        }
        public static string getSessionSummaryInfoFileData()
        {
            return File.ReadAllText(getSoftwareDataDir(false) + "\\SummaryInfo.txt", System.Text.Encoding.UTF8);
        }
        public static bool SessionSummaryInfoFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\SummaryInfo.txt";
            return File.Exists(file);
        }

        public static string getCodeTimeEventsFile()
        {
            return getSoftwareDataDir(true) + "\\events.json";
        }

        public static bool CodeTimeEventsFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\events.json";
            return File.Exists(file);
        }

        public static string getCodeTimeEventsData()
        {
            try
            {
                return File.ReadAllText(getSoftwareDataDir(true) + "\\events.json", System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
                return new JsonArray().ToString();
            }
        }

        public static string getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir(true) + "\\data.json";
        }
        public static bool LogFileExists()
        {
            string file = getSoftwareDataDir(true) + "\\Log.txt";
            return File.Exists(file);
        }
        public static string getLogFile()
        {
            return getSoftwareDataDir(true) + "\\Log.txt";
        }

        public static List<string> GetOfflinePayloadList()
        {

            List<string> jsonLines = new List<string>();
            string datastoreFile = getSoftwareDataStoreFile();
            if (File.Exists(datastoreFile))
            {
                // get the content
                string[] lines = File.ReadAllLines(datastoreFile, System.Text.Encoding.UTF8);

                if (lines != null && lines.Length > 0)
                {

                    foreach (string line in lines)
                    {
                        if (line != null && line.Trim().Length > 0)
                        {
                            string cleanedLine = SoftwareCoUtil.CleanJsonString(line);
                            jsonLines.Add(cleanedLine);
                        }
                    }
                }
            }
            return jsonLines;
        }

        public static long getItemAsLong(string key)
        {
            object val = getItem(key);
            if (val != null)
            {
                return Convert.ToInt64(val);
            }
            return 0L;
        }

        public static string getItemAsString(string key)
        {
            object val = getItem(key);
            if (val != null)
            {
                try
                {
                    return val.ToString();
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;
        }

        public static string getItemAsString(string key, string defaultVal)
        {
            object val = getItem(key);
            if (val != null)
            {
                try
                {
                    return val.ToString();
                }
                catch (Exception)
                {
                    return defaultVal;
                }
            }
            return defaultVal;
        }

        public static bool getItemAsBool(string key)
        {
            object val = getItem(key);
            if (val != null)
            {
                try
                {
                    return Convert.ToBoolean(val);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return false;
        }

        public static object getItem(string key)
        {
            // read the session json file
            string sessionFile = getSoftwareSessionFile();
            try
            {
                if (File.Exists(sessionFile))
                {
                    string content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);

                    if (content != null)
                    {

                        object jsonVal = SimpleJson.GetValue(content, key);
                        if (jsonVal != null)
                        {
                            return jsonVal;
                        }
                    }
                }
            }
            catch (Exception)
            {
                //
            }
            return null;
        }

        public static void setBoolItem(string key, bool val)
        {
            SaveSessionItem(key, val);
        }

        public static void setNumericItem(string key, long val)
        {
            SaveSessionItem(key, val);
        }

        public static void setItem(string key, string val)
        {
            SaveSessionItem(key, val);
        }

        public static void SaveSessionItem(string key, object val)
        {
            string sessionFile = getSoftwareSessionFile();

            try
            {
                string content = "";
                IDictionary<string, object> dict = new Dictionary<string, object>();
                if (File.Exists(sessionFile))
                {

                    content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);
                    content = SoftwareCoUtil.CleanJsonToDeserialize(content);
                    dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                }
                dict[key] = val;
                content = JsonConvert.SerializeObject(dict);

                File.WriteAllText(sessionFile, content, System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }

        public static string getPluginUuid()
        {
            string plugin_uuid = "";
            try
            {
                string content = getDeviceFileContent();

                object jsonVal = content != null ? SimpleJson.GetValue(content, "plugin_uuid") : null;
                if (jsonVal != null)
                {
                    plugin_uuid = jsonVal.ToString();
                }
                if (string.IsNullOrEmpty(plugin_uuid))
                {
                    plugin_uuid = Guid.NewGuid().ToString();
                    setPluginUuid(plugin_uuid);
                }
            }
            catch (Exception)
            {
                //
            }
            return plugin_uuid;
        }

        private static void setPluginUuid(string value)
        {
            writeContentToDeviceFile("plugin_uuid", value);
        }

        public static string getAuthCallbackState(bool autoCreate)
        {
            string auth_callback_state = "";
            try
            {
                string content = getDeviceFileContent();

                object jsonVal = content != null ? SimpleJson.GetValue(content, "auth_callback_state") : null;
                if (jsonVal != null)
                {
                    auth_callback_state = jsonVal.ToString();
                }
                if (string.IsNullOrEmpty(auth_callback_state) && autoCreate)
                {
                    auth_callback_state = Guid.NewGuid().ToString();
                    setAuthCallbackState(auth_callback_state);
                }
            }
            catch (Exception)
            {
                //
            }
            return auth_callback_state;
        }

        public static void setAuthCallbackState(string value)
        {
            writeContentToDeviceFile("auth_callback_state", value);
        }

        public static List<Integration> GetIntegrations()
        {
            List<Integration> integrations = new List<Integration>();
            // deserialize JSON directly from a file
            try
            {
                string file = getIntegrationsFile();
                string content = "";
                // IDictionary<string, object> dict = new Dictionary<string, object>();
                if (File.Exists(file))
                {

                    content = File.ReadAllText(file, Encoding.UTF8);
                    content = SoftwareCoUtil.CleanJsonToDeserialize(content);
                    integrations = JsonConvert.DeserializeObject<List<Integration>>(content);
                }
            } catch (Exception e) {
                Logger.Warning("Error reading integrations file: " + e.Message);
            }

            return integrations;
        }

        public static void syncIntegrations(List<Integration> integrations)
        {
            try
            {
                string content = JsonConvert.SerializeObject(integrations);
                File.WriteAllText(getIntegrationsFile(), content, Encoding.UTF8);
            }
            catch (Exception)
            {
                //
            }
            finally
            {

            }
        }

        private static string getDeviceFileContent()
        {
            string deviceFile = getDeviceFile();
            if (!File.Exists(deviceFile))
            {
                createDeviceFile();
            }
            try
            {
                string content = File.ReadAllText(deviceFile, System.Text.Encoding.UTF8);

                return content;
            }
            catch (Exception)
            {
                //
            }
            return null;
        }

        private static void createDeviceFile()
        {
            string deviceFile = getDeviceFile();
            if (!File.Exists(deviceFile))
            {
                // create it
                // set the plugin_uuid
                string plugin_uuid = Guid.NewGuid().ToString();
                writeContentToDeviceFile("plugin_uuid", plugin_uuid);
            }
        }

        private static void writeContentToDeviceFile(string key, string value)
        {
            string deviceFile = getDeviceFile();
            try
            {
                string content = "";
                IDictionary<string, object> dict = new Dictionary<string, object>();
                if (File.Exists(deviceFile))
                {

                    content = File.ReadAllText(deviceFile, System.Text.Encoding.UTF8);
                    content = SoftwareCoUtil.CleanJsonToDeserialize(content);
                    dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                }
                dict[key] = value;
                content = JsonConvert.SerializeObject(dict);

                File.WriteAllText(deviceFile, content, System.Text.Encoding.UTF8);
            }
            catch (Exception)
            {
            }
        }
    }


}
