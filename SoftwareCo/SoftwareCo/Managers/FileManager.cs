using System;
using System.Collections.Generic;
using System.Threading;
using System.IO;

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

        public static PluginData GetLastSavedKeystrokeStats()
        {
            string offlinePluginData = GetOfflinePayloadsAsString();
            if (offlinePluginData != null && !offlinePluginData.Equals(""))
            {
                JsonArray jsonArrayObj = (JsonArray)SimpleJson.DeserializeObject(offlinePluginData, new JsonArray());
                long latestStart = 0;
                foreach (JsonObject jsonObj in jsonArrayObj)
                {
                    PluginData td = PluginData.BuildFromDictionary(jsonObj);
                    if (td.start > latestStart)
                    {
                        lastSavedKeystrokeStats = td;
                    }
                }
            }

            return lastSavedKeystrokeStats;
        }

        public static String getDashboardFile()
        {
            return getSoftwareDataDir(true) + "\\CodeTime.txt";
        }

        public static String GetContributorDashboardFile()
        {
            return getSoftwareDataDir(true) + "\\ProjectContributorCodeSummary.txt";
        }

        public static String getVSReadmeFile()
        {
            return getSoftwareDataDir(true) + "\\VS_README.txt";
        }

        public static String getSoftwareDataDir(bool autoCreate)
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (autoCreate && !Directory.Exists(softwareDataDir))
            {
                try
                {
                    // create it
                    Directory.CreateDirectory(softwareDataDir);
                }
                catch (Exception ex)
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
        public static String getSessionSummaryFile()
        {
            return getSoftwareDataDir(true) + "\\sessionSummary.json";
        }

        public static String getSessionSummaryFileData()
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

        public static String getSoftwareSessionFile()
        {
            return getSoftwareDataDir(true) + "\\session.json";
        }

        public static bool FileChangeInfoSummaryFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\fileChangeSummary.json";
            return File.Exists(file);
        }
        public static String getFileChangeInfoSummaryData()
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

        public static String getFileChangeInfoSummaryFile()
        {
            return getSoftwareDataDir(true) + "\\fileChangeSummary.json";
        }

        public static String getSessionSummaryInfoFile()
        {
            return getSoftwareDataDir(true) + "\\SummaryInfo.txt";
        }
        public static String getSessionSummaryInfoFileData()
        {
            return File.ReadAllText(getSoftwareDataDir(false) + "\\SummaryInfo.txt", System.Text.Encoding.UTF8);
        }
        public static bool SessionSummaryInfoFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\SummaryInfo.txt";
            return File.Exists(file);
        }

        public static String getCodeTimeEventsFile()
        {
            return getSoftwareDataDir(true) + "\\events.json";
        }

        public static bool CodeTimeEventsFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\events.json";
            return File.Exists(file);
        }

        public static String getCodeTimeEventsData()
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

        public static String getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir(true) + "\\data.json";
        }
        public static bool LogFileExists()
        {
            string file = getSoftwareDataDir(true) + "\\Log.txt";
            return File.Exists(file);
        }
        public static String getLogFile()
        {
            return getSoftwareDataDir(true) + "\\Log.txt";
        }

        public static string GetOfflinePayloadsAsString()
        {
            string jsonData = null;
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
                    List<String> jsonLines = new List<string>();
                    foreach (string line in lines)
                    {
                        if (line != null && line.Trim().Length > 0)
                        {
                            jsonLines.Add(line);
                        }
                    }
                    jsonData = "[" + string.Join(",", jsonLines) + "]";
                }
            }
            return jsonData;
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
                catch (Exception e)
                {
                    return false;
                }
            }
            return false;
        }

        public static object getItem(string key)
        {
            // read the session json file
            string sessionFile = FileManager.getSoftwareSessionFile();
            lock (sessionFileLock)
            {
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
                catch (Exception e)
                {
                    //
                }
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

        public static void setItem(String key, string val)
        {
            SaveSessionItem(key, val);
        }

        public static void SaveSessionItem(string key, object val)
        {
            string sessionFile = FileManager.getSoftwareSessionFile();

            lock (sessionFileLock)
            {
                try
                {
                    string content = "";
                    IDictionary<string, object> dict = new Dictionary<string, object>();
                    if (File.Exists(sessionFile))
                    {

                        content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);

                        // convert to dictionary
                        dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content, new Dictionary<string, object>());
                        dict.Remove(key);
                    }
                    dict.Add(key, val);
                    content = SimpleJson.SerializeObject(dict);
                    // write it back to the file
                    content = content.Replace("\r\n", string.Empty).Replace("\n", string.Empty).Replace("\r", string.Empty);

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
