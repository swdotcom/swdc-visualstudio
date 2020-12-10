using Newtonsoft.Json;
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

        public static async Task<bool> IsOnlineAsync()
        {
            long nowInSec = SoftwareCoUtil.GetNowInSeconds();
            long thresholdSeconds = nowInSec - lastOnlineCheck;
            if (thresholdSeconds > 60)
            {
                // 3 second timeout
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null, null, false);
                isOnline = SoftwareHttpManager.IsOk(response);
                lastOnlineCheck = nowInSec;
            }

            return isOnline;
        }

        public static async Task<string> CreateAnonymousUserAsync(bool ignoreJwt)
        {
            // get the app jwt
            try
            {
                string jwt = FileManager.getItemAsString("jwt");
                if (String.IsNullOrEmpty(jwt))
                {
                    string plugin_uuid = FileManager.getPluginUuid();
                    string auth_callback_state = FileManager.getAuthCallbackState();
                    if (String.IsNullOrEmpty(auth_callback_state))
                    {
                        auth_callback_state = Guid.NewGuid().ToString();
                        FileManager.setAuthCallbackState(auth_callback_state);
                    }
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
                    jsonObj.Add("plugin_uuid", plugin_uuid);
                    jsonObj.Add("auth_callback_state", auth_callback_state);

                    string api = "/plugins/onboard";
                    string jsonData = jsonObj.ToString();
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, api, jsonData, null, false);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();

                        IDictionary<string, object> respObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                        respObj.TryGetValue("jwt", out object jwtObj);
                        jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                        if (jwt != null)
                        {
                            FileManager.setItem("jwt", jwt);
                            FileManager.setBoolItem("switching_account", false);
                            FileManager.setAuthCallbackState(null);
                            return jwt;
                        }
                    }
                }
            }
            catch (Exception ex)
            {

                Logger.Error("CreateAnonymousUserAsync, error: " + ex.Message, ex);
            }


            return null;
        }

        private static async Task<IDictionary<string, object>> GetUserFromResponseAsync(HttpResponseMessage response)
        {
            if (!SoftwareHttpManager.IsOk(response))
            {
                return null;
            }

            try
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody, new Dictionary<string, object>());
                if (jsonObj != null)
                {
                    jsonObj.TryGetValue("user", out object userObj);
                    if (userObj != null)
                    {
                        IDictionary<string, object> userData = (IDictionary<string, object>)userObj;
                        return userData;
                    }
                }
            } catch (Exception e) { }
            return null;
        }

        public static async Task<bool> IsLoggedOn()
        {
            string jwt = FileManager.getItemAsString("jwt");
            string authType = FileManager.getItemAsString("authType");

            string auth_callback_state = FileManager.getAuthCallbackState();

            string api = "/users/plugin/state";

            string token = (!String.IsNullOrEmpty(auth_callback_state)) ? auth_callback_state : jwt;
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, null, token);

            // check to see if we found the user or not
            IDictionary < string, object> user = await GetUserFromResponseAsync(response);

            if (user == null && !String.IsNullOrEmpty(authType) && (authType.Equals("software") || authType.Equals("email")))
            {
                // use the jwt
                response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, null, jwt);
                user = await GetUserFromResponseAsync(response);
            }

            if (user != null)
            {
                user.TryGetValue("email", out object emailObj);
                string email = (emailObj == null) ? null : Convert.ToString(emailObj);
                user.TryGetValue("registered", out object registeredObj);
                int registered = (registeredObj == null) ? 0 : Convert.ToInt32(registeredObj);
                user.TryGetValue("plugin_jwt", out object pluginJwtObj);
                string pluginJwt = (pluginJwtObj == null) ? null : Convert.ToString(pluginJwtObj);

                if (registered == 1)
                {
                    // set the name since we found a registered user
                    FileManager.setItem("name", email);
                }

                if (String.IsNullOrEmpty(authType))
                {
                    // default to software if auth type is null or empty
                    FileManager.setItem("authType", "software");
                }

                if (!String.IsNullOrEmpty(pluginJwt))
                {
                    // set the jwt since its found
                    FileManager.setItem("jwt", pluginJwt);
                }

                FileManager.setBoolItem("switching_account", false);
                FileManager.setAuthCallbackState(null);

                return true;
            }

            return false;
        }

        public static async void RefetchUserStatusLazily(int tryCountUntilFoundUser)
        {
            checkingLoginState = true;
            try
            {
                bool loggedIn = await IsLoggedOn();

                if (!loggedIn)
                {
                    if (tryCountUntilFoundUser > 0)
                    {
                        tryCountUntilFoundUser -= 1;

                        Task.Delay(1000 * 10).ContinueWith((task) => { RefetchUserStatusLazily(tryCountUntilFoundUser); });
                    } else
                    {
                        // clear the auth, we've tried enough
                        FileManager.setBoolItem("switching_account", false);
                        FileManager.setAuthCallbackState(null);
                        checkingLoginState = false;
                    }
                }
                else
                {
                    checkingLoginState = false;
                    // clear the auth, we've tried enough
                    FileManager.setBoolItem("switching_account", false);
                    FileManager.setAuthCallbackState(null);

                    // disable login command
                    SoftwareLoginCommand.UpdateEnabledState(false);
                    // enable web dashboard command
                    SoftwareLaunchCommand.UpdateEnabledState(true);

                    // clear the time data summary and session summary
                    SessionSummaryManager.Instance.ÇlearSessionSummaryData();
                    TimeDataManager.Instance.ClearTimeDataSummary();

                    // fetch the session summary to get the user's averages
                    WallclockManager.UpdateSessionSummaryFromServerAsync(true);

                    // show they've logged on
                    string msg = "Successfully logged on to Code Time.";
                    const string caption = "Code Time";
                    MessageBox.Show(msg, caption, MessageBoxButtons.OK);

                }
            }
            catch (Exception ex)
            {
                Logger.Error("RefetchUserStatusLazily ,error : " + ex.Message, ex);
                checkingLoginState = false;
            }

        }
    }
}
