
using Commons.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
// using SpotifyAPI.Local;
// using SpotifyAPI.Local.Enums;
// using SpotifyAPI.Local.Models;

namespace SoftwareCo
{
    class SoftwareCoUtil
    {
        private static bool _telemetryOn = true;

        /**
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

        public string RunCommand(String cmd, String dir)
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/c " + cmd;
            process.StartInfo.WorkingDirectory = dir;
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
            return "";
        }

        public bool IsOk(HttpResponseMessage response)
        {
            return (response != null && response.StatusCode == HttpStatusCode.OK);
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpMethod httpMethod, string uri, string optionalPayload)
        {

            if (!_telemetryOn)
            {
                return null;
            }

            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
            var cts = new CancellationTokenSource();
            HttpResponseMessage response = null;
            object jwt = getItem("jwt");
            if (jwt != null)
            {
                // add the authorizationn
                client.DefaultRequestHeaders.Add("Authorization", (string)jwt);
            }
            HttpContent contentPost = null;
            if (optionalPayload != null)
            {
                contentPost = new StringContent(optionalPayload, Encoding.UTF8, "application/json");
            }
            bool isPost = (httpMethod.Equals(HttpMethod.Post));
            try
            {
                string endpoint = Constants.api_endpoint + "" + uri;
                if (isPost)
                {
                    response = await client.PostAsync(endpoint, contentPost, cts.Token);
                }
                else
                {
                    response = await client.GetAsync(endpoint, cts.Token);
                }
            }
            catch (HttpRequestException e)
            {
                if (isPost)
                {
                    NotifyPostException(e);
                }
            }
            catch (TaskCanceledException e)
            {
                if (e.CancellationToken == cts.Token)
                {
                    // triggered by the caller
                    if (isPost)
                    {
                        NotifyPostException(e);
                    }
                }
                else
                {
                    // a web request timeout (possibly other things!?)
                    Logger.Info("We are having trouble receiving a response from Software.com");
                }
            }
            catch (Exception e)
            {
                if (isPost)
                {
                    NotifyPostException(e);
                }
            }
            finally
            {
            }
            return response;
        }

        private static void NotifyPostException(Exception e)
        {
            Logger.Error("We are having trouble sending data to Software.com, reason: " + e.Message);
        }

        public void UpdateTelemetry(bool isOn)
        {
            _telemetryOn = isOn;
        }

        public bool isTelemetryOn()
        {
            return _telemetryOn;
        }

        public object getItem(string key)
        {
            // read the session json file
            string sessionFile = getSoftwareSessionFile();
            if (File.Exists(sessionFile))
            {
                string content = File.ReadAllText(sessionFile);
                if (content != null)
                {
                    object val = SimpleJson.GetValue(content, key);
                    if (val != null)
                    {
                        return val;
                    }
                }
            }
            return null;
        }

        public void setItem(String key, object val)
        {
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
        
        public String getSoftwareDataDir()
        {
            String userHomeDir = Environment.ExpandEnvironmentVariables("%HOMEPATH%");
            string softwareDataDir = userHomeDir + "\\.software";
            if (!Directory.Exists(softwareDataDir))
            {
                // create it
                Directory.CreateDirectory(softwareDataDir);
            }
            return softwareDataDir;
        }

        public String getSoftwareSessionFile()
        {
            return getSoftwareDataDir() + "\\session.json";
        }

        public String getSoftwareDataStoreFile()
        {
            return getSoftwareDataDir() + "\\data.json";
        }

        public void launchSoftwareDashboard()
        {
            string url = Constants.url_endpoint;
            object tokenVal = this.getItem("token");
            object jwtVal = this.getItem("jwt");

            bool addedToken = false;
            if (tokenVal == null || ((string)tokenVal).Equals(""))
            {
                tokenVal = createToken();
                this.setItem("token", tokenVal);
                addedToken = true;
            }
            else if (jwtVal == null || ((string)jwtVal).Equals(""))
            {
                addedToken = true;
            }

            if (addedToken)
            {
                url += "/login?token=" + (string)tokenVal;
                RetrieveAuthTokenTimeout(60000);
            }

            System.Diagnostics.Process.Start(url);
        }

        public string createToken()
        {
            return System.Guid.NewGuid().ToString().Replace("-", "");
        }

        public async void RetrieveAuthTokenTimeout(int millisToWait)
        {
            await Task.Delay(millisToWait);
            RetrieveAuthToken();
        }

        public async void RetrieveAuthToken()
        {
            object token = this.getItem("token");
            string jwt = null;
            HttpResponseMessage response = await this.SendRequestAsync(HttpMethod.Get, "/users/plugin/confirm?token=" + token, null);
            if (this.IsOk(response))
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(responseBody);
                jsonObj.TryGetValue("jwt", out object jwtObj);
                jwt = (jwtObj == null) ? null : Convert.ToString(jwtObj);

                if (jwt != null)
                {
                    this.setItem("jwt", jwt);
                }

                this.setItem("vs_lastUpdateTime", getNowInSeconds());
            }

            if (jwt == null)
            {
                RetrieveAuthTokenTimeout(120000);
            }
        }

        public long getNowInSeconds()
        {
            long unixSeconds = DateTimeOffset.Now.ToUnixTimeSeconds();
            return unixSeconds;
        }
    }

    
}
