

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
// using SpotifyAPI.Local;
// using SpotifyAPI.Local.Enums;
// using SpotifyAPI.Local.Models;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        private static bool _telemetryOn = true;
        private static IDictionary<string, string> sessionMap = new Dictionary<string, string>();
        public static int DASHBOARD_LABEL_WIDTH = 25;
        public static int DASHBOARD_VALUE_WIDTH = 25;

        /***
        private SpotifyLocalAPI _spotify = null;

        public IDictionary<string, string> getTrackInfo()
        {
            IDictionary<string, string> dict = new Dictionary<string, string>();

            if (_spotify == null)
            {
                _spotify = new SpotifyLocalAPI();
            }

            if (SpotifyLocalAPI.IsSpotifyRunning() && _spotify.Connect())
            {
                StatusResponse status = _spotify.GetStatus();
                Logger.Info("got spotify status: " + status.ToString());
            }

            return dict;
        }
        **/

        public static string RunCommand(String cmd, String dir)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = "/c " + cmd;
                if (dir != null)
                {
                    process.StartInfo.WorkingDirectory = dir;
                }
                process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                //* Read the output (or the error)
                string output = process.StandardOutput.ReadToEnd();
                string err = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (output != null)
                {
                    return output.Trim();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Code Time: Unable to execute command, error: " + e.Message);
            }
            return "";
        }

        public static void UpdateTelemetry(bool isOn)
        {
            _telemetryOn = isOn;
        }

        public static bool isTelemetryOn()
        {
            return _telemetryOn;
        }

        public static string getHostname()
        {
            string hostname = SoftwareCoUtil.RunCommand("hostname", null);
            return hostname;
        }

        public static object getItem(string key)
        {
            sessionMap.TryGetValue(key, out string valObject);
            string val = (valObject == null) ? null : valObject;
            if (val != null)
            {
                return val;
            }

            // read the session json file
            string sessionFile = getSoftwareSessionFile();
            if (File.Exists(sessionFile))
            {
                string content = File.ReadAllText(sessionFile);
                if (content != null)
                {
                    object jsonVal = SimpleJson.GetValue(content, key);
                    if (jsonVal != null)
                    {
                        return jsonVal;
                    }
                }
            }
            return null;
        }

        public static void setItem(String key, string val)
        {
            if (sessionMap.TryGetValue(key, out string outval))
            {
                // yay, value exists!
                sessionMap[key] = val;
            }
            else
            {
                // darn, lets add the value
                sessionMap.Add(key, val);
            }


            string sessionFile = getSoftwareSessionFile();
            IDictionary<string, object> dict = new Dictionary<string, object>();
            string content = "";
            if (File.Exists(sessionFile))
            {
                content = File.ReadAllText(sessionFile);
                // conver to dictionary
                dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content);
                dict.Remove(key);
            }
            dict.Add(key, val);
            content = SimpleJson.SerializeObject(dict);
            // write it back to the file
            File.WriteAllText(sessionFile, content);
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                return Regex.IsMatch(email, @"\S+@\S+\.\S+",
                    RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static String getDashboardFile()
        {
            return getSoftwareDataDir(true) + "\\CodeTime.txt";
        }

        public static String getSoftwareDataDir(bool autoCreate)
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (autoCreate && !Directory.Exists(softwareDataDir))
            {
                // create it
                Directory.CreateDirectory(softwareDataDir);
            }
            return softwareDataDir;
        }

        public static bool softwareSessionFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\session.json";
            return File.Exists(file);
        }

        public static bool jwtExists()
        {
            string jwt = SoftwareUserSession.GetJwt();
            return (jwt != null && !jwt.Equals(""));
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
            return File.ReadAllText(getSoftwareDataDir(true)+"\\sessionSummary.json");
        }
        public static String getSoftwareSessionFile()
        {
            return getSoftwareDataDir(true) + "\\session.json";
        }

        
        public static String getSessionSummaryInfoFile()
        {
            return getSoftwareDataDir(true) + "\\SummaryInfo.txt";
        }
        public static String getSessionSummaryInfoFileData()
        {
            return  File.ReadAllText(getSoftwareDataDir(false) + "\\SummaryInfo.txt");
        }
        public static bool SessionSummaryInfoFileExists()
        {
            string file = getSoftwareDataDir(false) + "\\SummaryInfo.txt";
            return File.Exists(file);
        }
      
        public static String getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir(true) + "\\data.json";
        }
        

        public static string getSectionHeader( string  label)
        {
            string result = "";
            string content = label + "\n";
            string dash = "";
          
            int dashLen = DASHBOARD_LABEL_WIDTH + DASHBOARD_VALUE_WIDTH + 15;
            for (int i = 0; i < dashLen; i++)
            {
                dash += "-";
            }
            
            return result = content + dash +"\n";
        }
        public static void launchSoftwareTopForty()
        {
            string url = "https://api.software.com/music/top40";
            Process.Start(url);
        }

        public static void launchWebDashboard()
        {
            string url = Constants.url_endpoint;
            Process.Start(url);
        }

        public static async void launchLogin()
        {
            bool isOnline = await SoftwareUserSession.IsOnlineAsync();
            string jwt = SoftwareUserSession.GetJwt();
            if (jwt == null && isOnline)
            {
                // initialize the anon flow
                await SoftwareUserSession.CreateAnonymousUserAsync(isOnline);
            }
            jwt = SoftwareUserSession.GetJwt();
            string url = Constants.url_endpoint + "/onboarding?token=" + jwt;
            if (!isOnline)
            {
                // just show the app home, which should end up showing up with a no connection message
                url = Constants.url_endpoint;
            }
            
            Process.Start(url);

            if (!isOnline)
            {
                return;
            }

            if (!SoftwareUserSession.checkingLoginState)
            {
                SoftwareUserSession.RefetchUserStatusLazily(12);
            }

        }

        public static NowTime GetNowTime()
        {
            NowTime timeParam = new NowTime();   
            timeParam.now = DateTimeOffset.Now.ToUnixTimeSeconds();
            timeParam.offset_now = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            timeParam.local_now = timeParam.now + ((int)timeParam.offset_now * 60);
    
            return timeParam;
        }
        public static long getNowInSeconds()
        {
            long unixSeconds = DateTimeOffset.Now.ToUnixTimeSeconds();
            return unixSeconds;
        }

        public static long GetBeginningOfDay(DateTime now)
        {
            DateTime begOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            return ((DateTimeOffset)begOfToday).ToUnixTimeSeconds();
        }

        public static long GetEndOfDay(DateTime now)
        {
            DateTime begOfToday = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            return ((DateTimeOffset)begOfToday).ToUnixTimeSeconds();
        }

        public static string FormatNumber(float number)
        {
            string numberStr = "";
            if (number >= 1000 || number % 1 == 0)
            {
                numberStr = String.Format("{0:n0}", number);
            }
            else
            {
                numberStr = String.Format("{0:0.00}", number);
            }
            return numberStr;
        }

        public static string HumanizeMinutes(long minutes)
        {
            string minutesStr = "";
            if (minutes == 60)
            {
                minutesStr = "1 hr";
            }
            else if (minutes > 60)
            {
                string formatedHrs;
                float hours = (float)minutes / 60;
                if (hours % 1 == 0)
                {
                    formatedHrs = String.Format("{0:n0}", hours);
                }
                else
                {
                    formatedHrs = String.Format("{0:0.00}", hours);
                }
                minutesStr = formatedHrs + " hrs";
            }
            else if (minutes == 1)
            {
                minutesStr = "1 min";
            }
            else
            {
                minutesStr = minutes + " min";
            }
            return minutesStr;
        }

        public static string GetCurrentSessionIcon(double currentSessionGoalPercentVal)
        {
            string sessionTimeIcon = "";
            if (currentSessionGoalPercentVal > 0)
            {
                if (currentSessionGoalPercentVal < 0.4)
                {
                    sessionTimeIcon = "🌘";
                }
                else if (currentSessionGoalPercentVal < 0.7)
                {
                    sessionTimeIcon = "🌗";
                }
                else if (currentSessionGoalPercentVal < 0.93)
                {
                    sessionTimeIcon = "🌖";
                }
                else if (currentSessionGoalPercentVal < 1.3)
                {
                    sessionTimeIcon = "🌕";
                }
                else
                {
                    sessionTimeIcon = "🌔";

                }
            }
            return sessionTimeIcon;
        }

       public static string getDashboardRow(string label, string value)
        {
            string result = "";
            result =  getDashboardLabel(label) +":"+  getDashboardValue(value)+ "\n";
            return result;

        }

        private static string getDashboardLabel(string label)
        {
           return  getDashboardDataDisplay(DASHBOARD_VALUE_WIDTH, label);
        }

        private static string getDashboardValue(string value)
        {
            string valueContent = getDashboardDataDisplay(DASHBOARD_VALUE_WIDTH, value);
            string  paddedContent = "";
            for (int i = 0; i < 11; i++)
            {
                paddedContent += " ";
            }
            paddedContent += valueContent;
            return paddedContent;
        }
        private static string getDashboardDataDisplay(int dASHBOARD_VALUE_WIDTH, string data)
        {
            int len = dASHBOARD_VALUE_WIDTH - data.Length;        
            string content = "";
            for (int i = 0; i < len; i++)
            {
                content += " ";
            }

            return content += data;
        }

        internal static string CreateDateSuffix(DateTime date)
        {
           
                // Get day...
                var day = date.Day;

                // Get day modulo...
                var dayModulo = day % 10;

                // Convert day to string...
                var suffix = day.ToString(System.Globalization.CultureInfo.InvariantCulture);

                // Combine day with correct suffix...
                suffix += (day == 11 || day == 12 || day == 13) ? "th" :
                    (dayModulo == 1) ? "st" :
                    (dayModulo == 2) ? "nd" :
                    (dayModulo == 3) ? "rd" :
                    "th";

                // Return result...
                return suffix;
            
        }

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        public static void WriteToFileThreadSafe(string text, string path)
        {
            // Set Status to Locked
            _readWriteLock.EnterWriteLock();
            try
            {
                // Append text to the file
                File.WriteAllText(path, text);
                File.SetAttributes(path, FileAttributes.ReadOnly);
            }
            finally
            {
                // Release lock
                _readWriteLock.ExitWriteLock();
            }
        }
    }
   
    struct Date
    {
        public static double GetTime(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static DateTime DateTimeParse(double milliseconds)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(milliseconds).ToLocalTime();
        }

    }
    class NowTime
    {
        public long now { get; set; }
        public long local_now { get; set; }
        public double offset_now { get; set; }
    }

}
