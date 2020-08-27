using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SoftwareCo
{
    class FileManager
    {

        private static PluginData lastSavedKeystrokeStats = null;
        private static Object sessionFileLock = new object();
        private static Object dataFileLock = new object();

        public static void ClearLastSavedKeystrokeStats()
        {
            lastSavedKeystrokeStats = null;
        }

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
                {
                    // 
                }

            }
            return softwareDataDir;
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

        public static string getSessionSummaryFileData()
        {
            try
            {
                return File.ReadAllText(getSoftwareDataDir(true) + "\\sessionSummary.json", System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                return new JsonObject().ToString();
            }
        }

        public static string getSoftwareSessionFile()
        {
            return getSoftwareDataDir(true) + "\\session.json";
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
            catch (Exception e)
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
            catch (Exception e)
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
                string[] lines = null;
                lock (dataFileLock)
                {
                    lines = File.ReadAllLines(datastoreFile, System.Text.Encoding.UTF8);
                }

                if (lines != null && lines.Length > 0)
                {
                    
                    foreach (string line in lines)
                    {
                        if (line != null && line.Trim().Length > 0)
                        {
                            jsonLines.Add(line);
                        }
                    }
                }
            }
            return jsonLines;
        }

        public static void AppendPluginData(string pluginDataContent)
        {
            string datastoreFile = getSoftwareDataStoreFile();
            lock (dataFileLock)
            {
                // append to the file
                File.AppendAllText(datastoreFile, pluginDataContent + Environment.NewLine);
            }
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
                catch (Exception e)
                {
                    return null;
                }
            }
            return null;
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

            lock (sessionFileLock)
            {
                try
                {
                    string content = "";
                    IDictionary<string, object> dict = new Dictionary<string, object>();
                    if (File.Exists(sessionFile))
                    {

                        content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);
                        dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    }
                    dict[key] = val;
                    content = JsonConvert.SerializeObject(dict);

                    File.WriteAllText(sessionFile, content, System.Text.Encoding.UTF8);
                }
                catch (Exception e)
                {
                    //
                }
            }
        }
    }

    
}
