using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace SoftwareCo
{
    class SoftwareUserSession
    {

        private static bool loggedInCacheState = false;
        private static string lastJwt = null;
        public static bool checkingLoginState = false;
        public static bool isOnline = true;

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
            // 3 second timeout
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null, 3, null, true /*isOnlineCheck*/);
            isOnline = SoftwareHttpManager.IsOk(response);

            return isOnline;
        }

        public static string GetJwt()
        {
            if (lastJwt == null)
            {
                object jwt = SoftwareCoUtil.getItem("jwt");
                lastJwt = (jwt != null && !((string)jwt).Equals("")) ? (string)jwt : null;
            }
            return lastJwt;
        }

        public static async Task<string> CreateAnonymousUserAsync(bool online)
        {
            // get the app jwt
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
                    IDictionary<string, object> respObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                    respObj.TryGetValue("jwt", out object jwtObj);
                    string jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                    if (jwt != null)
                    {
                        SoftwareCoUtil.setItem("jwt", jwt);
                        return jwt;
                    }
                }
            }

            return null;
        }

        public static async Task<string> GetAppJwtAsync(bool online)
        {
            if (online)
            {
                long seconds = SoftwareCoUtil.getNowInSeconds();
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                        HttpMethod.Get, "/data/apptoken?token=" + seconds, null);

                if (SoftwareHttpManager.IsOk(response))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                    jsonObj.TryGetValue("jwt", out object jwtObj);
                    string app_jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                    return app_jwt;
                }
            }
            return null;
        }

        private static async Task<User> GetUserAsync(bool online)
        {
            string jwt = GetJwt();
            if (jwt != null && online)
            {
                string api = "/users/me";
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, jwt);
                if (SoftwareHttpManager.IsOk(response))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
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
            return null;
        }

        private static async Task<bool> IsLoggedOn(bool online)
        {
            string jwt = GetJwt();
            if (online && jwt != null)
            {
                User user = await GetUserAsync(online);
                if (user != null && SoftwareCoUtil.IsValidEmail(user.email))
                {
                    SoftwareCoUtil.setItem("name", user.email);
                    SoftwareCoUtil.setItem("jwt", user.plugin_jwt);
                    lastJwt = user.plugin_jwt;
                    return true;
                }

                string api = "/users/plugin/state";
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, jwt);
                if (SoftwareHttpManager.IsOk(response))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
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
                                SoftwareCoUtil.setItem("name", name);
                            }
                            SoftwareCoUtil.setItem("jwt", pluginJwt);
                            lastJwt = pluginJwt;
                        }
                        else if (state.Equals("NOT_FOUND"))
                        {
                            SoftwareCoUtil.setItem("jwt", null);
                            lastJwt = null;
                        }
                    }
                }

            }
            SoftwareCoUtil.setItem("name", null);
            return false;
        }

        public static async Task<UserStatus> GetUserStatusAsync(bool isInitialCall)
        {
            bool online = await IsOnlineAsync();
            bool softwareSessionFileExists = SoftwareCoUtil.softwareSessionFileExists();
            if (!isInitialCall && isOnline && !softwareSessionFileExists)
            {
                await SoftwareUserSession.CreateAnonymousUserAsync(online);
            }

            bool loggedIn = await IsLoggedOn(online);

            UserStatus currentUserStatus = new UserStatus();
            currentUserStatus.loggedIn = loggedIn;

            if (online && loggedInCacheState != loggedIn)
            {
                // change in logged in state, send heatbeat and fetch kpm
                SendHeartbeat("STATE_CHANGE:LOGGED_IN:" + loggedIn);

                try
                {
                    Thread.Sleep(1000);
                    SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
                } catch (ThreadInterruptedException e)
                {
                    //
                }
            }

            loggedInCacheState = loggedIn;

            SoftwareLaunchCommand.UpdateEnabledState(currentUserStatus);
            SoftwareLoginCommand.UpdateEnabledState(currentUserStatus);

            return currentUserStatus;
        }

        public static async void RefetchUserStatusLazily(int tryCountUntilFoundUser)
        {
            checkingLoginState = true;
            UserStatus userStatus = await GetUserStatusAsync(true);

            if (!userStatus.loggedIn && tryCountUntilFoundUser > 0)
            {
                tryCountUntilFoundUser -= 1;
                
                try
                {
                    Thread.Sleep(1000 * 10);
                    RefetchUserStatusLazily(tryCountUntilFoundUser);
                } catch (ThreadInterruptedException e)
                {
                    //
                }
            }
            else
            {
                SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
                checkingLoginState = false;
            }
        }

        public static async void SendHeartbeat(string reason)
        {
            string jwt = GetJwt();
            bool online = await IsOnlineAsync();
            if (online && jwt != null)
            {

                string version = Constants.EditorVersion;

                JsonObject jsonObj = new JsonObject();
                jsonObj.Add("version", SoftwareCoPackage.GetVersion());
                jsonObj.Add("os", SoftwareCoPackage.GetOs());
                jsonObj.Add("pluginId", Constants.PluginId);
                jsonObj.Add("start", SoftwareCoUtil.getNowInSeconds());
                jsonObj.Add("trigger_annotation", reason);
                jsonObj.Add("hostname", SoftwareCoUtil.getHostname());

                string api = "/data/heartbeat";
                string jsonData = jsonObj.ToString();
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, api, jsonData, jwt);

                if (!SoftwareHttpManager.IsOk(response))
                {
                    Logger.Warning("Code Time: Unable to send heartbeat");
                }
            }
        }
    }
}
