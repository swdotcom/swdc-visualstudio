


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

    public static string workspace_name = Guid.NewGuid().ToString();

    public static string GetFirstCommandResult(string cmd, string dir)
    {
      List<string> result = RunCommand(cmd, dir);
      string firstResult = result != null && result.Count > 0 ? result[0] : null;
      return firstResult;
    }

    public static List<string> RunCommand(string cmd, string dir)
    {
      List<string> result = CacheManager.GetCmdResultCachedValue(dir, cmd);
      if (result != null)
      {
        return result;
      }
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
        List<string> output = new List<string>();

        while (process.StandardOutput.Peek() > -1)
        {
          output.Add(process.StandardOutput.ReadLine().TrimEnd());
        }

        while (process.StandardError.Peek() > -1)
        {
          output.Add(process.StandardError.ReadLine().TrimEnd());
        }
        process.WaitForExit();

        // all of the callers are expecting a 1 line response. return the 1st line
        if (output.Count > 0)
        {
          CacheManager.UpdateCmdResult(dir, cmd, output);
          return output;
        }
      }
      catch (Exception e)
      {
        Logger.Error("Code Time: Unable to execute command, error: " + e.Message);
      }
      return null;
    }

    public static List<string> GetCommandResultList(string cmd, string dir)
    {
      List<string> resultList = RunCommand(cmd, dir);
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
      return GetFirstCommandResult("hostname", null);
    }

    public static IDictionary<string, object> ConvertObjectToSource(IDictionary<string, object> dict)
    {
      dict.TryGetValue("source", out object sourceJson);
      try
      {
        IDictionary<string, object> sourceData = (sourceJson == null) ? null : (IDictionary<string, object>)sourceJson;
        return sourceData;
      }
      catch (Exception e)
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

    public static string GetStringVal(IDictionary<string, object> dict, string key)
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

    public static long GetLongVal(IDictionary<string, object> dict, string key)
    {
      if (!dict.ContainsKey(key))
      {
        return 0;
      }
      try
      {
        return Convert.ToInt64(dict[key]);
      }
      catch (Exception e)
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

    public static string getSectionHeader(string label)
    {
      string result = "";
      string content = label + "\n";
      string dash = "";

      int dashLen = DASHBOARD_LABEL_WIDTH + DASHBOARD_VALUE_WIDTH + 15;
      for (int i = 0; i < dashLen; i++)
      {
        dash += "-";
      }

      return result = content + dash + "\n";
    }

    public static void launchWebDashboard()
    {
      string url = Constants.url_endpoint;
      Process.Start(url);
    }

    public static void launchCodeTimeDashboard()
    {
      Process.Start(Constants.url_endpoint + "/dashboard/code_time?view=summary");
    }

    public static void launchReadme()
    {
      Process.Start("https://www.github.com/swdotcom/swdc-visualstudio");
    }

    public static void launchSettings()
    {
      Process.Start(Constants.url_endpoint + "/preferences");
    }

    public static void launchMailToCody()
    {
      string url = Constants.cody_email_url;
      Process.Start(url);
    }

    public static async void launchLogin(string loginType, bool switching_account)
    {
      try
      {
        string auth_callback_state = FileManager.getAuthCallbackState(true);
        FileManager.setAuthCallbackState(auth_callback_state);
        FileManager.setItem("authType", loginType);

        JsonObject jsonObj = new JsonObject();
        jsonObj.Add("plugin", "codetime");
        jsonObj.Add("plugin_uuid", FileManager.getPluginUuid());
        jsonObj.Add("pluginVersion", EnvUtil.GetVersion());
        jsonObj.Add("plugin_id", EnvUtil.getPluginId());
        jsonObj.Add("auth_callback_state", auth_callback_state);

        string jwt = FileManager.getItemAsString("jwt");
        string url = "";
        string element_name = "ct_sign_up_google_btn";
        string icon_name = "google";
        string cta_text = "Sign up with Google";
        if (loginType.Equals("google"))
        {
          jsonObj.Add("redirect", Constants.url_endpoint);
          url = Constants.api_endpoint + "/auth/google";
        }
        else if (loginType.Equals("github"))
        {
          jsonObj.Add("redirect", Constants.url_endpoint);
          element_name = "ct_sign_up_github_btn";
          icon_name = "github";
          cta_text = "Sign up with GitHub";
          url = Constants.api_endpoint + "/auth/github";
        }
        else
        {
          jsonObj.Add("token", jwt);
          jsonObj.Add("auth", "software");
          element_name = "ct_sign_up_email_btn";
          icon_name = "evelope";
          cta_text = "Sign up with email";
          url = Constants.url_endpoint + "/email-signup";
        }

        StringBuilder sb = new StringBuilder();
        // create the query string from the json object
        foreach (KeyValuePair<string, object> kvp in jsonObj)
        {
          if (sb.Length > 0)
          {
            sb.Append("&");
          }
          sb.Append(kvp.Key).Append("=").Append(System.Web.HttpUtility.UrlEncode(kvp.Value.ToString(), System.Text.Encoding.UTF8));
        }

        url += "?" + sb.ToString();

        Process.Start(url);

        UIElementEntity entity = new UIElementEntity();
        entity.color = null;
        entity.element_location = "ct_menu_tree";
        entity.element_name = element_name;
        entity.cta_text = cta_text;
        entity.icon_name = icon_name;
        TrackerEventManager.TrackUIInteractionEvent(UIInteractionType.click, entity);

        if (!SoftwareUserManager.checkingLoginState)
        {
          FileManager.setBoolItem("switching_account", switching_account);
          Task.Delay(1000 * 10).ContinueWith((task) => { SoftwareUserManager.RefetchUserStatusLazily(40); });
        }
      }
      catch (Exception ex)
      {

        Logger.Error("launchLogin, error : " + ex.Message, ex);
      }

    }

    public static string GetFormattedDay(long seconds)
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
      return dateTimeOffset.ToString(@"yyyy-MM-dd");
    }

    public static string ToRfc3339String(long seconds)
    {
      DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(seconds);
      return dateTimeOffset.ToString(@"yyyy-MM-dd'T'HH:mm:ssZ", DateTimeFormatInfo.InvariantInfo);
    }

    public static bool IsNewDay()
    {
      NowTime nowTime = GetNowTime();
      string currentDay = FileManager.getItemAsString("currentDay");
      return (!nowTime.day.Equals(currentDay)) ? true : false;
    }

    public static NowTime GetNowTime()
    {
      NowTime timeParam = new NowTime();
      timeParam.day = DateTime.Now.ToString(@"yyyy-MM-dd");
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
      }
      else
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
      string str;
      if (minutes == 60)
      {
        str = "1h";
      }
      else if (minutes > 60)
      {
        double hours = Math.Floor((float)minutes / 60);
        double remainder_minutes = (minutes % 60);
        string formatedHrs = String.Format("{0:0}", Math.Floor(hours)) + "h";
        if ((remainder_minutes / 60) % 1 == 0)
        {
          str = formatedHrs;
        }
        else
        {
          str = formatedHrs + " " + remainder_minutes + "m";
        }
      }
      else if (minutes == 1)
      {
        str = "1m";
      }
      else
      {
        str = minutes + "m";
      }
      return str;
    }

    public static string GetPercentOfReferenceAvg(long curr, long refval, string refvalDisplay)
    {
      curr = curr > 0 ? curr : 0;
      double quotient = 1;
      if (refval > 0)
      {
        quotient = (double)curr / refval;
        if (curr > 0 && quotient < 0.01)
        {
          quotient = 0.01;
        }
      }
      return quotient.ToString("P", CultureInfo.InvariantCulture);
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
      result = getDashboardLabel(label) + ":" + getDashboardValue(value) + "\n";
      return result;

    }

    private static string getDashboardLabel(string label)
    {
      return getDashboardDataDisplay(DASHBOARD_VALUE_WIDTH, label);
    }

    private static string getDashboardValue(string value)
    {
      string valueContent = getDashboardDataDisplay(DASHBOARD_VALUE_WIDTH, value);
      string paddedContent = "";
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
      image.Width = 16;
      image.Height = 16;

      BitmapImage bi = new BitmapImage();
      // BitmapImage.UriSource must be in a BeginInit/EndInit block.
      bi.BeginInit();
      bi.UriSource = new Uri(@"../Resources/" + iconName, UriKind.RelativeOrAbsolute);
      bi.EndInit();
      // Set the image source.
      image.Source = bi;

      return image;
    }

    public static bool IsGitProject(string projDir)
    {
      if (projDir == null || projDir.Equals(""))
      {
        return false;
      }
      string gitDir = projDir + "\\.git";
      bool hasGitDir = Directory.Exists(gitDir);
      return hasGitDir;
    }

    public static string CleanJsonToDeserialize(string json)
    {
      return json.Replace(@"\", string.Empty).Replace("\"[", "[").Replace("]\"", "]");
    }

    public static string CleanJsonString(string data)
    {
      // byte order mark clean up
      string BOMMarkUtf8 = Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble());
      if (data.StartsWith(BOMMarkUtf8))
      {
        data = data.Remove(0, BOMMarkUtf8.Length);
      }
      data = data.Replace("\r\n", "").Replace("\n", "").Replace("\r", "");

      int braceIdx = data.IndexOf("{");
      int bracketIdx = data.IndexOf("[");

      // multi editor writes to the data.json can
      // cause an undefined string before the json chars, remove it
      if (braceIdx > 0 && (braceIdx < bracketIdx || bracketIdx == -1))
      {
        data = data.Substring(braceIdx);
      }
      else if (bracketIdx > 0 && (bracketIdx < braceIdx || braceIdx == -1))
      {
        data = data.Substring(bracketIdx);
      }

      return data;
    }

    [STAThread]
    public static void ShowNotification(string title, string message)
    {
      Task.Delay(0).ContinueWith((task) =>
      {
        Notification.Show(title, message);
      });
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
    public string day { get; set; }
    public long local_start_of_day { get; set; }
    public long local_end_of_day { get; set; }
    public long utc_end_of_day { get; set; }
    public DateTime start_of_yesterday_dt { get; set; }
    public long local_start_of_yesterday { get; set; }
    public DateTime start_of_week_dt { get; set; }
    public long local_start_of_week { get; set; }

  }

}
