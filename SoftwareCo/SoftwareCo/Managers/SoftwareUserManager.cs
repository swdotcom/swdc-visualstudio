using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SoftwareCo
{
    class SoftwareUserManager
    {
        public static bool checkingLoginState = false;
        public static bool isOnline = true;
        public static long lastOnlineCheck = 0;

        public class UserStatus
        {
            public bool loggedIn;
        }

        public class User
        {
            public long id;
            public string email;
            public string plugin_jwt;
        }

        public static async Task<bool> IsOnlineAsync()
        {
            long nowInSec = SoftwareCoUtil.GetNowInSeconds();
            long thresholdSeconds = nowInSec - lastOnlineCheck;
            if (thresholdSeconds > 60) {
                // 3 second timeout
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null, 3, null, true /*isOnlineCheck*/);
                isOnline = SoftwareHttpManager.IsOk(response);
                lastOnlineCheck = nowInSec;
            }

            return isOnline;
        }

        public static async Task<string> CreateAnonymousUserAsync(bool online)
        {
            // get the app jwt
            try
            {
                string app_jwt = await GetAppJwtAsync(online);
                if (app_jwt != null && online)
                {
                    string creation_annotation = "NO_SESSION_FILE";
                    string osUsername = Environment.UserName;
                    string timezone = "";
                    if (TimeZone.CurrentTimeZone.DaylightName != null
                        && TimeZone.CurrentTimeZone.DaylightName != TimeZone.CurrentTimeZone.StandardName)
                    {
                        timezone = TimeZone.CurrentTimeZone.DaylightName;
                    }
                    else
                    {
                        timezone = TimeZone.CurrentTimeZone.StandardName;
                    }

                    JsonObject jsonObj = new JsonObject();
                    jsonObj.Add("timezone", timezone);
                    jsonObj.Add("username", osUsername);
                    jsonObj.Add("hostname", SoftwareCoUtil.getHostname());
                    jsonObj.Add("creation_annotation", creation_annotation);

                    string api = "/data/onboard";
                    string jsonData = jsonObj.ToString();
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, api, jsonData, app_jwt);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> respObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                        respObj.TryGetValue("jwt", out object jwtObj);
                        string jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                        if (jwt != null)
                        {
                            FileManager.setItem("jwt", jwt);
                            return jwt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.Error("CreateAnonymousUserAsync, error: " + ex.Message,ex);
            }
           

            return null;
        }

        public static async Task<string> GetAppJwtAsync(bool online)
        {
            try
            {
                if (online)
                {
                    long seconds = SoftwareCoUtil.GetNowInSeconds();
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                            HttpMethod.Get, "/data/apptoken?token=" + seconds, null);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                        jsonObj.TryGetValue("jwt", out object jwtObj);
                        string app_jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                        return app_jwt;
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.Error("GetAppJwtAsync, error: " + ex.Message, ex);
            }
          
            return null;
        }

        private static async Task<User> GetUserAsync(bool online)
        {
            string jwt = FileManager.getItemAsString("jwt");
            try
            {
                if (jwt != null && online)
                {
                    string api = "/users/me";
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, jwt);
                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                        if (jsonObj != null)
                        {
                            jsonObj.TryGetValue("data", out object userObj);
                            if (userObj != null)
                            {
                                IDictionary<string, object> userData = (IDictionary<string, object>)userObj;

                                userData.TryGetValue("email", out object emailObj);
                                string email = (emailObj == null) ? null : Convert.ToString(emailObj);
                                userData.TryGetValue("plugin_jwt", out object pluginJwtObj);
                                string pluginJwt = (pluginJwtObj == null) ? null : Convert.ToString(pluginJwtObj);
                                userData.TryGetValue("id", out object idObj);
                                long userId = (idObj == null) ? 0L : Convert.ToInt64(idObj);

                                User user = new User();
                                user.email = email;
                                user.plugin_jwt = pluginJwt;
                                user.id = userId;
                                return user;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("GetUserAsync, error: " + ex.Message, ex);

            }
           
            return null;
        }

        public static async Task<bool> IsLoggedOn(bool online)
        {
            try
            {
                string jwt = FileManager.getItemAsString("jwt");
                if (online && jwt != null)
                {
                    User user = await GetUserAsync(online);
                    if (user != null && SoftwareCoUtil.IsValidEmail(user.email))
                    {
                        FileManager.setItem("name", user.email);
                        FileManager.setItem("jwt", user.plugin_jwt);
                        return true;
                    }

                    string api = "/users/plugin/state";
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, jwt);
                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                        if (jsonObj != null)
                        {
                            jsonObj.TryGetValue("state", out object stateObj);
                            string state = (stateObj == null) ? "NONE" : Convert.ToString(stateObj);
                            jsonObj.TryGetValue("jwt", out object pluginJwtObj);
                            string pluginJwt = (pluginJwtObj == null) ? null : Convert.ToString(pluginJwtObj);
                            if (state.Equals("OK") && pluginJwt != null)
                            {
                                jsonObj.TryGetValue("email", out object nameObj);
                                string name = (nameObj == null) ? null : Convert.ToString(nameObj);
                                if (name != null)
                                {
                                    FileManager.setItem("name", name);
                                }
                                FileManager.setItem("jwt", pluginJwt);
                            }
                            else if (state.Equals("NOT_FOUND"))
                            {
                                FileManager.setItem("jwt", null);
                            }
                        }
                    }

                }
                FileManager.setItem("name", null);
            }
            catch (Exception ex)
            {
                //
            }  
          
            return false;
        }

        public static async void RefetchUserStatusLazily(int tryCountUntilFoundUser)
        {
            try
            {
                bool loggedIn = await IsLoggedOn(true);

                if ( !loggedIn && tryCountUntilFoundUser > 0)
                {
                    tryCountUntilFoundUser -= 1;

                    Task.Delay(1000 * 10).ContinueWith((task) => { RefetchUserStatusLazily(tryCountUntilFoundUser); });
                }
                else
                {
                    checkingLoginState = false;
                    if (loggedIn)
                    {
                        SoftwareLoginCommand.UpdateEnabledState(true);
                        SoftwareLaunchCommand.UpdateEnabledState(true);
                        // show they've logged on
                        string msg = "Successfully logged on to Code Time.";
                        const string caption = "Code Time";
                        MessageBox.Show(msg, caption, MessageBoxButtons.OK);

                        // fetch the session summary to get the user's averages
                        WallclockManager.Instance.UpdateSessionSummaryFromServerAsync();

                        SoftwareCoPackage.SendOfflinePluginBatchData();
                        
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("RefetchUserStatusLazily ,error : " + ex.Message, ex);
              
            }
            
        }
    }
}
