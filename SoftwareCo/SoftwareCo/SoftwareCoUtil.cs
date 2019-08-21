

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
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

        public static String getSoftwareSessionFile()
        {
            return getSoftwareDataDir(true) + "\\session.json";
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

        public static String getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir(true) + "\\data.json";
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


}
