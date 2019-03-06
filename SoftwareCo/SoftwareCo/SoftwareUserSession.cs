using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;

namespace SoftwareCo
{
    class SoftwareUserSession
    {

        private static Regex macAddrRegex = new Regex("^([0-9A-Fa-f]{2}[:-]?){5}([0-9A-Fa-f]{2})$");

        public class UserStatus
        {
            public User loggedInUser;
            public string email;
            public bool hasAccounts;
            public bool hasUserAccounts;
        }

        public class User
        {
            public long id;
            public string email;
            public string plugin_jwt;
            public string mac_addr;
            public string mac_addr_share;
        }

        public static async Task<bool> RequiresUserCreationAsync()
        {
            string sessionFile = SoftwareCoUtil.getSoftwareSessionFile();
            bool hasSessionFile = (File.Exists(sessionFile)) ? true : false;
            bool hasJwt = HasJwt();
            bool online = await IsOnlineAsync();

            if (online && (!hasSessionFile || !hasJwt))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> IsOnlineAsync()
        {
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null);
            return SoftwareHttpManager.IsOk(response);
        }
        public static async Task<bool> IsAuthenticatedAsync()
        {
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/users/ping", null);
            return SoftwareHttpManager.IsOk(response);
        }
        public static bool HasJwt()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            return (jwt != null && !((string)jwt).Equals(""));
        }
        public static string GetJwt()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            return (jwt != null && !((string)jwt).Equals("")) ? (string)jwt : null;
        }
        public static async Task CreateAnonymousUserAsync()
        {
            // get the app jwt
            string app_jwt = await GetAppJwtAsync();
            // get the jwt
            string jwt = GetJwt();
            // get the mac addr
            string macAddr = GetMacAddress();
            if (app_jwt != null && jwt == null && macAddr != null)
            {
                string token = SoftwareCoUtil.GetNewOrExistingToken();
                string email = macAddr;
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
                jsonObj.Add("email", email);
                jsonObj.Add("plugin_token", token);

                string api = "/data/onboard?addr=" + WebUtility.UrlEncode(macAddr);
                string jsonData = jsonObj.ToString();
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, api, jsonData, app_jwt);

                if (SoftwareHttpManager.IsOk(response))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> respObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                    respObj.TryGetValue("jwt", out object jwtObj);
                    jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);
                    if (jwt != null)
                    {
                        SoftwareCoUtil.setItem("jwt", jwt);
                    }

                    respObj.TryGetValue("user", out object userObj);
                    string userStr = (userObj == null) ? null : Convert.ToString(userObj);
                    if (userStr != null)
                    {
                        SoftwareCoUtil.setItem("user", userStr);
                    }

                    SoftwareCoUtil.setItem("vs_lastUpdateTime", SoftwareCoUtil.getNowInSeconds());
                }
            }
        }

        public static string GetMacAddress()
        {
            string osUsername = Environment.UserName;

            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            DateTime userHomeDirCreateTime = Directory.GetCreationTime(userHomeDir);

            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddr = string.Empty;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string tempMac = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        macAddr += string.Format("{0}", bytes[i].ToString("x2"));
                        if (i != bytes.Length - 1)
                        {
                            macAddr += ":";
                        }
                    }
                    break;
                }
            }
            

            string identifier = osUsername + "_" + macAddr + "_" + Date.GetTime(userHomeDirCreateTime);
            return identifier;
        }
        public static async Task<string> GetAppJwtAsync()
        {
            object appJwt = SoftwareCoUtil.getItem("app_jwt");
            bool hasAppJwt = (appJwt != null && !((string)appJwt).Equals(""));
            bool online = await IsOnlineAsync();

            if (!hasAppJwt && online)
            {
                string macAddress = GetMacAddress();
                if (macAddress != null)
                {
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                            HttpMethod.Get, "/data/token?addr=" + WebUtility.UrlEncode(macAddress), null);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                        jsonObj.TryGetValue("jwt", out object jwtObj);
                        string app_jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);

                        if (app_jwt != null)
                        {
                            SoftwareCoUtil.setItem("app_jwt", app_jwt);
                        }
                    }
                }
            }
            appJwt = SoftwareCoUtil.getItem("app_jwt");
            if (appJwt != null)
            {
                return (string)appJwt;
            }
            return null;
        }

        private static async Task<List<User>> GetAuthenticatedPluginAccountsAsync(string token)
        {
            List<User> users = new List<User>();
            object jwtObj = SoftwareCoUtil.getItem("jwt");
            object appJwt = SoftwareCoUtil.getItem("app_jwt");
            string authenticatingJwt = (jwtObj != null) ? (string)jwtObj : (string)appJwt;
            string macAddress = GetMacAddress();
            string tokenQryStr = "";
            if (token == null)
            {
                // most cases token should be null
                tokenQryStr = "?token=" + WebUtility.UrlEncode(macAddress);
            } else
            {
                tokenQryStr = "?token=" + token;
            }

            bool online = await IsOnlineAsync();
            if (authenticatingJwt != null && online)
            {
                string api = "/users/plugin/accounts" + tokenQryStr;
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, api, null);
                if (SoftwareHttpManager.IsOk(response))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                    JsonArray usersArr = null;
                    if (jsonObj != null)
                    {
                        jsonObj.TryGetValue("users", out object usersObj);
                        usersArr = (usersObj == null) ? null : (JsonArray)usersObj;
                        if (usersArr != null && usersArr.Count > 0)
                        {
                            foreach (object userObj in usersArr)
                            {
                                IDictionary<string, object> userData = (IDictionary<string, object>)userObj;
                                userData.TryGetValue("email", out object emailObj);
                                string email = (emailObj == null) ? null : Convert.ToString(emailObj);
                                userData.TryGetValue("mac_addr", out object macAddrObj);
                                string macAddr = (macAddrObj == null) ? null : Convert.ToString(macAddrObj);
                                userData.TryGetValue("mac_addr_share", out object macAddrShareObj);
                                string macAddrShare = (macAddrShareObj == null) ? null : Convert.ToString(macAddrShareObj);
                                userData.TryGetValue("plugin_jwt", out object pluginJwtObj);
                                string pluginJwt = (pluginJwtObj == null) ? null : Convert.ToString(pluginJwtObj);
                                userData.TryGetValue("id", out object idObj);
                                long userId = (idObj == null) ? 0L : Convert.ToInt64(idObj);

                                User user = new User();
                                user.email = email;
                                user.mac_addr = macAddr;
                                user.mac_addr_share = macAddrShare;
                                user.plugin_jwt = pluginJwt;
                                user.id = userId;
                                users.Add(user);
                            }
                        }
                    }

                }
            }

            return users;
        }

        private static User GetLoggedInUser(List<User> authAccounts)
        {
            string macAddress = GetMacAddress();
            if (authAccounts != null && authAccounts.Count > 0)
            {
                User loggedInUser = null;
                User secondaryUser = null;
                User anonAccount = null;
                foreach (User user in authAccounts)
                {
                    string userMacAddr = user.mac_addr;
                    string userEmail = user.email;
                    string userMacAddrShare = user.mac_addr_share;
                    if (userMacAddr != null && userEmail != null && !userEmail.Equals(userMacAddr) && userMacAddr.Equals(macAddress))
                    {
                        loggedInUser = user;
                        break;
                    } else if (!userEmail.Equals(userMacAddrShare))
                    {
                        secondaryUser = user;
                    } else if (anonAccount == null && (userEmail.Equals(userMacAddr) || userEmail.Equals(macAddress)))
                    {
                        anonAccount = user;
                    }
                }

                if (loggedInUser != null)
                {
                    updateSessionUserInfo(loggedInUser);
                    return loggedInUser;
                } else if (anonAccount != null)
                {
                    updateSessionUserInfo(anonAccount);
                } else if (secondaryUser != null)
                {
                    updateSessionUserInfo(secondaryUser);
                }
            }

            return null;
        }

        private static void updateSessionUserInfo(User user)
        {
            JsonObject jsonObj = new JsonObject();
            jsonObj.Add("id", user.id);
            SoftwareCoUtil.setItem("user", jsonObj.ToString());
            SoftwareCoUtil.setItem("jwt", user.plugin_jwt);
            SoftwareCoUtil.setItem("vs_lastUpdateTime", SoftwareCoUtil.getNowInSeconds());
        }

        private static bool HasRegisteredAccounts(List<User> authAccounts)
        {
            string macAddress = GetMacAddress();
            if (authAccounts != null && authAccounts.Count > 0)
            {
                foreach (User user in authAccounts)
                {
                    if (!user.email.Equals(macAddress) && !user.email.Equals(user.mac_addr))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool HasPluginAccounts(List<User> authAccounts)
        {
            if (authAccounts != null && authAccounts.Count > 0)
            {
                return true;
            }
            return false;
        }


        public static async Task<UserStatus> GetUserStatusAsync(string token)
        {
            UserStatus userStatus = new UserStatus();
            List<User> authAccounts = await GetAuthenticatedPluginAccountsAsync(token);
            User loggedInUser = GetLoggedInUser(authAccounts);
            bool hasUserAccounts = HasRegisteredAccounts(authAccounts);
            bool hasAccounts = HasPluginAccounts(authAccounts);

            userStatus.email = (loggedInUser != null) ? loggedInUser.email : null;
            userStatus.loggedInUser = loggedInUser;
            userStatus.hasAccounts = hasAccounts;
            userStatus.hasUserAccounts = hasUserAccounts;

            return userStatus;
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
            } else
            {
                SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
            }
        }

        public static async void PluginLogout()
        {
            string jwt = GetJwt();
            string api = "/users/plugin/logout";
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, api, null);

            await GetUserStatusAsync(null);
            SoftwareCoPackage.ProcessFetchDailyKpmTimerCallbackAsync(null);
        }

    }
}
