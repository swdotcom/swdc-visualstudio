using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace SoftwareCo
{
    class SoftwareUserSession
    {

        private static bool loggedInCacheState = false;

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
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null);
            return SoftwareHttpManager.IsOk(response);
        }

        public static string GetJwt()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            return (jwt != null && !((string)jwt).Equals("")) ? (string)jwt : null;
        }

        public static async Task CreateAnonymousUserAsync(bool online)
        {
            // get the app jwt
            string app_jwt = await GetAppJwtAsync(online);
            if (app_jwt != null && online)
            {
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
                    }
                }
            }
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

        private static async User GetUserAsync(bool online)
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

        private static async bool IsLoggedOn(bool online)
        {
            string jwt = GetJwt();
            if (online && jwt != null)
            {
                User user = await GetUserAsync(online);
                if (user != null && SoftwareCoUtil.IsValidEmail(user.email))
                {
                    SoftwareCoUtil.setItem("name", user.email);
                    SoftwareCoUtil.setItem("jwt", user.plugin_jwt);
                    return true;
                }

                string api = "/usrs/plugin/state";
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
                        }
                        else if (!state.Equals("ANONYMOUS"))
                        {
                            SoftwareCoUtil.setItem("jwt", null);
                        }
                    }
                }

            }
            SoftwareCoUtil.setItem("name", null);
            return false;
        }

        public static async Task<UserStatus> GetUserStatusAsync(string token)
        {
            SoftwareCoUtil.CleanSessionInfo();

            string jwt = GetJwt();

            bool online = await IsOnlineAsync();

            if (jwt == null)
            {
                await CreateAnonymousUserAsync(online);
            }

            bool loggedIn = await IsLoggedOn(online);

            // the jwt may have been nulled out
            jwt = GetJwt();
            if (jwt == null)
            {
                await CreateAnonymousUserAsync(online);
            }

            UserStatus currentUserStatus = new UserStatus();
            currentUserStatus.loggedIn = loggedIn;

            if (loggedInCacheState != loggedIn)
            {
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
                });
            }

            loggedInCacheState = loggedIn;

            return currentUserStatus;
        }

        public static async void RefetchUserStatusLazily(int tryCountUntilFoundUser)
        {
            UserStatus userStatus = await GetUserStatusAsync(null);
            if (userStatus.loggedInUser == null && tryCountUntilFoundUser > 0)
            {
                tryCountUntilFoundUser -= 1;
                Thread t = new Thread(() =>
                {
                    Thread.Sleep(1000 * 10);
                    RefetchUserStatusLazily(tryCountUntilFoundUser);
                });
            }
            else
            {
                SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
            }
        }

    }
}
