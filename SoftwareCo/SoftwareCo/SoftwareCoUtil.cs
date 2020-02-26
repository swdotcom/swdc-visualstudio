﻿

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        private static bool _telemetryOn = true;
        private static IDictionary<string, string> sessionMap = new Dictionary<string, string>();
        public static int DASHBOARD_LABEL_WIDTH = 25;
        public static int DASHBOARD_VALUE_WIDTH = 25;


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

        public static List<string> GetCommandResultList(string cmd, string dir)
        {
            List<string> resultList = new List<string>();
            string commandResult = SoftwareCoUtil.RunCommand(cmd, dir);

            if (commandResult != null && !commandResult.Equals(""))
            {
                string[] lines = commandResult.Split(
                    new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                resultList = new List<string>(lines);
            }
            return resultList;
        }

        public static RepoResourceInfo GetResourceInfo(string projectDir)
        {
            RepoResourceInfo info = new RepoResourceInfo();
            try
            {
                string identifier = SoftwareCoUtil.RunCommand("git config remote.origin.url", projectDir);
                if (identifier != null && !identifier.Equals(""))
                {
                    info.identifier = identifier;

                    // only get these since identifier is available
                    string email = SoftwareCoUtil.RunCommand("git config user.email", projectDir);
                    if (email != null && !email.Equals(""))
                    {
                        info.email = email;

                    }
                    string branch = SoftwareCoUtil.RunCommand("git symbolic-ref --short HEAD", projectDir);
                    if (branch != null && !branch.Equals(""))
                    {
                        info.branch = branch;
                    }
                    string tag = SoftwareCoUtil.RunCommand("git describe --all", projectDir);

                    if (tag != null && !tag.Equals(""))
                    {
                        info.tag = tag;
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error("GetResourceInfo , error :" + ex.Message, ex);

            }


            return info;
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

        public static long getItemAsLong(string key)
        {
            object val = getItem(key);
            if (val != null)
            {
                return long.Parse(val.ToString());
            }
            return 0l;
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
            return null;
        }

        public static void setNumericItem(string key, long val)
        {
            string sessionFile = getSoftwareSessionFile();
            IDictionary<string, object> dict = new Dictionary<string, object>();
            string content = "";
            if (File.Exists(sessionFile))
            {
                content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);
                // conver to dictionary
                dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content);
                dict.Remove(key);
            }
            dict.Add(key, val);
            content = SimpleJson.SerializeObject(dict);
            // write it back to the file
            File.WriteAllText(sessionFile, content, System.Text.Encoding.UTF8);
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
                content = File.ReadAllText(sessionFile, System.Text.Encoding.UTF8);
                // conver to dictionary
                dict = (IDictionary<string, object>)SimpleJson.DeserializeObject(content);
                dict.Remove(key);
            }
            dict.Add(key, val);
            content = SimpleJson.SerializeObject(dict);
            // write it back to the file
            File.WriteAllText(sessionFile, content,System.Text.Encoding.UTF8);
        }

        public static T DictionaryToObject<T>(IDictionary<string, object> dict) where T : new()
        {
            var t = new T();
            PropertyInfo[] properties = t.GetType().GetProperties();

            foreach (PropertyInfo property in properties)
            {
                if (!dict.Any(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase)))
                    continue;

                KeyValuePair<string, object> item = dict.First(x => x.Key.Equals(property.Name, StringComparison.InvariantCultureIgnoreCase));

                // Find which property type (int, string, double? etc) the CURRENT property is...
                Type tPropertyType = t.GetType().GetProperty(property.Name).PropertyType;

                // Fix nullables...
                Type newT = Nullable.GetUnderlyingType(tPropertyType) ?? tPropertyType;

                // ...and change the type
                object newA = Convert.ChangeType(item.Value, newT);
                t.GetType().GetProperty(property.Name).SetValue(t, newA, null);
            }
            return t;
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
                try
                {
                    // create it
                    Directory.CreateDirectory(softwareDataDir);
                }
                catch (Exception ex)
                {

                    
                }
               
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
            return File.ReadAllText(getSoftwareDataDir(true)+"\\sessionSummary.json", System.Text.Encoding.UTF8);
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
            return  File.ReadAllText(getSoftwareDataDir(false) + "\\SummaryInfo.txt",System.Text.Encoding.UTF8);
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
        public static bool LogFileExists()
        {
            string file = getSoftwareDataDir(true) + "\\Log.txt";
            return File.Exists(file);
        }
        public static String getLogFile()
        {
            return getSoftwareDataDir(true) + "\\Log.txt";
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
            try
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
            catch (Exception ex)
            {

                Logger.Error("launchLogin, error : " + ex.Message, ex);
            }
            

        }

        public static NowTime GetNowTime()
        {
            NowTime timeParam = new NowTime();
            DateTimeOffset offset = DateTimeOffset.Now;
            // utc now in seconds
            timeParam.now = offset.ToUnixTimeSeconds();
            // set the offset (will be negative before utc and positive after)
            timeParam.offset_minutes = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            timeParam.offset_seconds = timeParam.offset_minutes * 60;
            // local now in seconds
            timeParam.local_now = Convert.ToInt64(timeParam.now + timeParam.offset_seconds);
            timeParam.local_day = offset.ToLocalTime().ToString(@"yyyy-MM-dd");

            // start and end of day
            timeParam.local_start_of_day = Convert.ToInt64(StartOfDay() + timeParam.offset_seconds);
            timeParam.local_end_of_day = Convert.ToInt64(EndOfDay() + timeParam.offset_seconds);
            timeParam.utc_end_of_day = EndOfDay();

            return timeParam;
        }

        public static long GetNowInSeconds()
        {
            long unixSeconds = DateTimeOffset.Now.ToUnixTimeSeconds();
            return unixSeconds;
        }

        public static long EndOfDay()
        {
            DateTime now = DateTime.Now;
            DateTime endOfDay = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59);
            return ((DateTimeOffset)endOfDay).ToUnixTimeSeconds();
        }

        public static long StartOfDay()
        {
            DateTime now = DateTime.Now;
            DateTime begOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            return ((DateTimeOffset)begOfToday).ToUnixTimeSeconds();
        }

        public static string FormatNumber(double number)
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
                float hours = (float)minutes / 60;
                string formatedHrs = String.Format("{0:0.00}", hours);
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

        public static string NormalizeGithubEmail(string email)
        {
            Regex regex = new Regex(@"^[0-9]+[\+]");
            if (email != null)
            {
                email = Regex.Replace(email, @"users.noreply", "");
                if (regex.IsMatch(email))
                {
                    email = email.Substring(email.IndexOf("+") + 1);
                }
            }
            return email;
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
        /// <summary>
        /// Function Equibalent to setTimeout 
        /// </summary>
        /// <param name="interval"> Time interval to call function</param>
        /// <param name="function"> Genarliaze function parameter </param>
        /// <param name="value">Boolean value to call as a setInterval method </param>
        public static void SetTimeout(int interval, Action function, bool value)
        {
            Action functionCopy = (Action)function.Clone(); // if incoming function set to null it could get crashed need to copy it before hand
            System.Timers.Timer timer = new System.Timers.Timer { Interval = interval, AutoReset = value };
            timer.Elapsed += (sender, e) => functionCopy();
            timer.Start();
        }

        public static Image CreateImage(string iconName)
        {
            // create Image
            Image image = new Image();
            image.Source = new BitmapImage(new Uri("Resources/" + iconName, UriKind.Relative));
            return image;
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
        public double offset_minutes { get; set; }
        public double offset_seconds { get; set; }
        public string local_day { get; set; }
        public long local_start_of_day { get; set; }
        public long local_end_of_day { get; set; }
        public long utc_end_of_day { get; set; }

    }
    
}
