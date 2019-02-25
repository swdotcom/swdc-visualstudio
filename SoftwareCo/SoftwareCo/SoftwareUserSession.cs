using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SoftwareCo
{
    class SoftwareUserSession
    {

        private Regex macAddrRegex = new Regex("^([0-9A-Fa-f]{2}[:-]?){5}([0-9A-Fa-f]{2})$");

        public async Task<bool> RequiresUserCreationAsync()
        {
            string sessionFile = SoftwareCoUtil.getSoftwareSessionFile();
            bool hasSessionFile = (File.Exists(sessionFile)) ? true : false;
            bool hasJwt = this.HasJwt();
            bool online = await this.IsOnlineAsync();

            if (online && (!hasSessionFile || !hasJwt))
            {
                return true;
            }

            return false;
        }

        public async Task<bool> IsOnlineAsync()
        {
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/ping", null);
            return SoftwareHttpManager.IsOk(response);
        }
        public async Task<bool> IsAuthenticatedAsync()
        {
            HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Get, "/users/ping", null);
            return SoftwareHttpManager.IsOk(response);
        }
        public bool HasJwt()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            return (jwt != null && !((string)jwt).Equals(""));
        }
        public string GetJwt()
        {
            object jwt = SoftwareCoUtil.getItem("jwt");
            return (jwt != null && !((string)jwt).Equals("")) ? (string)jwt : null;
        }
        public async Task CreateAnonymousUserAsync()
        {
            // get the app jwt
            string app_jwt = await this.GetAppJwtAsync();
            // get the jwt
            string jwt = this.GetJwt();
            // get the mac addr
            string macAddr = this.GetMacAddress();
            if (app_jwt != null && jwt == null && macAddr != null)
            {

            }
        }

        public string GetMacAddress()
        {
            string osUsername = Environment.UserName;

            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            DateTime userHomeDirCreateTime = Directory.GetCreationTime(userHomeDir);

            const int MIN_MAC_ADDR_LENGTH = 12;
            string macAddress = string.Empty;
            long maxSpeed = -1;

            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string tempMac = nic.GetPhysicalAddress().ToString();
                if (nic.Speed > maxSpeed &&
                    !string.IsNullOrEmpty(tempMac) &&
                    tempMac.Length >= MIN_MAC_ADDR_LENGTH)
                {
                    maxSpeed = nic.Speed;
                    macAddress = tempMac;
                }
            }

            string identifier = osUsername + "_" + macAddress + "_" + Date.GetTime(userHomeDirCreateTime);
            return identifier;
        }
        public async Task<string> GetAppJwtAsync()
        {
            object appJwt = SoftwareCoUtil.getItem("app_jwt");
            bool hasAppJwt = (appJwt != null && !((string)appJwt).Equals(""));
            bool online = await this.IsOnlineAsync();

            if (!hasAppJwt && online)
            {
                string macAddress = this.GetMacAddress();
                if (macAddress != null)
                {
                    HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(
                            HttpMethod.Get, "/data/token?addr=" + WebUtility.UrlEncode(macAddress), null);

                    if (SoftwareHttpManager.IsOk(response))
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                        jsonObj.TryGetValue("app_jwt", out object jwtObj);
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

    }
}
