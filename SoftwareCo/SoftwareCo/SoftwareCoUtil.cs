


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        private static bool _telemetryOn = true;
        public static int DASHBOARD_LABEL_WIDTH = 25;
        public static int DASHBOARD_VALUE_WIDTH = 25;
        public static long DAY_IN_SEC = 60 * 60 * 24;

        public static String workspace_name = Guid.NewGuid().ToString();

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
                resultList = new List<string>(lines);
            }
            return resultList;
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

        public static IDictionary<string, object> ConvertObjectToSource(IDictionary<string, object> dict)
        {
            dict.TryGetValue("source", out object sourceJson);
            try
            {
                IDictionary<string, object> sourceData = (sourceJson == null) ? null : (IDictionary<string, object>)sourceJson;
                return sourceData;
            } catch (Exception e)
            {
                //
            }
            return new Dictionary<string, object>();
        }

        public static PluginDataProject ConvertObjectToProject(IDictionary<string, object> dict)
        {
            dict.TryGetValue("project", out object projJson);
            try
            {
                JsonObject projJsonObj = (projJson == null) ? null : (JsonObject)projJson;
                if (projJson != null)
                {
                    return PluginDataProject.GetPluginDataFromDictionary(projJsonObj);
                }
            }
            catch (Exception e)
            {
                //
            }
            return new PluginDataProject("Unnamed", "Untitled");
        }

        public static string ConvertObjectToString(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return "";
            }
            try
            {
                return Convert.ToString(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return "";
        }

        public static long ConvertObjectToLong(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0;
            }
            try
            {
                return Convert.ToInt64(dict[key]);
            } catch (Exception e)
            {
                //
            }
            return 0;
        }

        public static bool ConvertObjectToBool(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return false;
            }
            try
            {
                return Convert.ToBoolean(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return false;
        }

        public static double ConvertObjectToDouble(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0.0;
            }
            try
            {
                return Convert.ToDouble(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return 0.0;
        }

        public static int ConvertObjectToInt(IDictionary<string, object> dict, string key)
        {
            if (!dict.ContainsKey(key))
            {
                return 0;
            }
            try
            {
                return Convert.ToInt32(dict[key]);
            }
            catch (Exception e)
            {
                //
            }
            return 0;
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

        public static void launchMailToCody()
        {
            string url = Constants.cody_email_url;
            Process.Start(url);
        }

        public static async void launchLogin(string loginType)
        {
            try
            {
                string jwt = FileManager.getItemAsString("jwt");
                string url = "";
                if (loginType.Equals("google"))
                {
                    url = Constants.api_endpoint + "/auth/google?token=" + jwt + "&plugin=codetime&redirect=" + Constants.url_endpoint;
                } else if (loginType.Equals("github"))
                {
                    url = Constants.api_endpoint + "/auth/github?token=" + jwt + "&plugin=codetime&redirect=" + Constants.url_endpoint;
                } else
                {
                    url = Constants.url_endpoint + "/email-signup?token=" + jwt + "&plugin=codetime&ath=software";
                }

                Process.Start(url);

                if (!SoftwareUserSession.checkingLoginState)
                {
                    SoftwareUserSession.RefetchUserStatusLazily(40);
                }
            }
            catch (Exception ex)
            {

                Logger.Error("launchLogin, error : " + ex.Message, ex);
            }

        }

        public static String GetFormattedDay(long seconds)
        {
            DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
            return dateTimeOffset.ToString(@"yyyy-MM-dd");
        }

        public static bool IsNewDay()
        {
            NowTime nowTime = SoftwareCoUtil.GetNowTime();
            string currentDay = FileManager.getItemAsString("currentDay");
            return (!nowTime.local_day.Equals(currentDay)) ? true : false;
        }

        public static NowTime GetNowTime()
        {
            NowTime timeParam = new NowTime();
            DateTimeOffset offset = DateTimeOffset.Now;
            // utc now in seconds
            timeParam.now = offset.ToUnixTimeSeconds();
            timeParam.now_dt = DateTime.Now;
            // set the offset (will be negative before utc and positive after)
            timeParam.offset_minutes = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
            timeParam.offset_seconds = timeParam.offset_minutes * 60;
            // local now in seconds
            timeParam.local_now = Convert.ToInt64(timeParam.now + timeParam.offset_seconds);
            timeParam.local_day = offset.ToLocalTime().ToString(@"yyyy-MM-dd");

            // start and end of day
            timeParam.start_of_today = StartOfDay();
            timeParam.local_start_of_day = Convert.ToInt64(((DateTimeOffset)timeParam.start_of_today).ToUnixTimeSeconds() + timeParam.offset_seconds);
            timeParam.local_end_of_day = Convert.ToInt64(EndOfDay() + timeParam.offset_seconds);
            timeParam.utc_end_of_day = EndOfDay();

            // yesterday start
            timeParam.start_of_yesterday_dt = StartOfYesterday();
            timeParam.local_start_of_yesterday = Convert.ToInt64(((DateTimeOffset)timeParam.start_of_yesterday_dt).ToUnixTimeSeconds() + timeParam.offset_seconds);

            // week start
            timeParam.start_of_week_dt = StartOfWeek();
            timeParam.local_start_of_week = Convert.ToInt64(((DateTimeOffset)timeParam.start_of_week_dt).ToUnixTimeSeconds() + timeParam.offset_seconds);

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

        public static DateTime StartOfDay()
        {
            DateTime now = DateTime.Now;
            DateTime begOfToday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            return begOfToday;
        }

        public static DateTime StartOfYesterday()
        {
            DateTime now = DateTime.Now;
            now = now.AddDays(-1);
            DateTime begOfYesterday = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            return begOfYesterday;
        }

        public static DateTime StartOfWeek()
        {
            DateTime now = DateTime.Now;
            DayOfWeek dow = DateTime.Now.DayOfWeek;
            if (dow == DayOfWeek.Sunday)
            {
                // subtract 7
                now = now.AddDays(-7);
            } else
            {
                // subtract until it equals sunday
                while (now.DayOfWeek != DayOfWeek.Sunday)
                {
                    now = now.AddDays(-1);
                }
            }
            DateTime begOfWeek = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
            // return ((DateTimeOffset)begOfWeek).ToUnixTimeSeconds();
            return begOfWeek;
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

        public static bool IsGitProject(string projDir)
        {
            if (projDir == null || projDir.Equals(""))
            {
                return false;
            }
            // string sessionFile = projDir + "\\.git";
            // return File.Exists(sessionFile);

            return true;
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
    public class NowTime
    {
        public long now { get; set; }
        public DateTime now_dt { get; set; }
        public DateTime start_of_today { get; set; }
        public long local_now { get; set; }
        public double offset_minutes { get; set; }
        public double offset_seconds { get; set; }
        public string local_day { get; set; }
        public long local_start_of_day { get; set; }
        public long local_end_of_day { get; set; }
        public long utc_end_of_day { get; set; }
        public DateTime start_of_yesterday_dt { get; set; }
        public long local_start_of_yesterday { get; set; }
        public DateTime start_of_week_dt { get; set; }
        public long local_start_of_week { get; set; }

    }
    
}
