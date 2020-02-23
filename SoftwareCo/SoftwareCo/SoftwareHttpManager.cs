using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace SoftwareCo
{
    class SoftwareHttpManager
    {
        private static SpotifyToken token = null;

        public static bool IsOk(HttpResponseMessage response)
        {
            return (response != null && response.StatusCode == HttpStatusCode.OK);
        }

        public static bool HasSpotifyAccessToken()
        {
            DateTime dt = DateTime.Now;
            // make sure we have a token and now is earlier than the expire date
            return token != null && dt.CompareTo(token.Expire_date) < 0 ? true : false;
        }

        public static async Task InitializeSpotifyClientGrantAsync()
        {
            try
            {
                token = new SpotifyToken();

                string clientId = "72e3067b2cfe4f04933668ab12140c19";
                string clientSecret = "4873269b09604d24a63d86f6a05dddb2";
                string spotifyAuth = string.Format("{0}:{1}", clientId, clientSecret);
                string encodedAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes(spotifyAuth));

                string spotifyUrl = "https://accounts.spotify.com/api/token";
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(spotifyUrl);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Accept = "application/json";
                webRequest.Headers.Add("Authorization: Basic " + encodedAuth);

                string request = ("grant_type=client_credentials");
                byte[] req_bytes = Encoding.ASCII.GetBytes(request);
                webRequest.ContentLength = req_bytes.Length;

                Stream stream = await webRequest.GetRequestStreamAsync();
                await stream.WriteAsync(req_bytes, 0, req_bytes.Length);
                stream.Close();

                HttpWebResponse resp = (HttpWebResponse)await webRequest.GetResponseAsync();
                string json = "";

                using (Stream respStr = resp.GetResponseStream())
                {
                    using (StreamReader rdr = new StreamReader(respStr, Encoding.UTF8))
                    {
                        // should get back a string we can turn into a json
                        json = await rdr.ReadToEndAsync();
                        rdr.Close();
                    }
                }

                IDictionary<string, object> jsonObj = (IDictionary<string, object>)SimpleJson.DeserializeObject(json);
                jsonObj.TryGetValue("access_token", out object accessTokenObj);
                string access_token = (accessTokenObj == null) ? null : Convert.ToString(accessTokenObj);
                jsonObj.TryGetValue("token_type", out object tokenTypeObj);
                string token_type = (tokenTypeObj == null) ? null : Convert.ToString(tokenTypeObj);
                // time period (in seconds) for which the access token is valid
                jsonObj.TryGetValue("expires_in", out object expiresInObj);
                double expires_in = (expiresInObj == null) ? 0d : Convert.ToDouble(expiresInObj);

                DateTime dt = DateTime.Now;
                dt = dt.AddSeconds(expires_in);

                token.Access_token = access_token;
                token.Token_type = token_type;
                token.Expires_in = expires_in;
                token.Expire_date = dt;
            } catch (Exception e)
            {
                token = null;
            }
        }

        public static async Task<HttpResponseMessage> SendDashboardRequestAsync(HttpMethod httpMethod, string uri)
        {
            return await SendRequestAsync(httpMethod, uri, null, 60);
        }

        public static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod httpMethod, string uri, string optionalPayload)
        {
            return await SendRequestAsync(httpMethod, uri, optionalPayload, 10);
        }

        public static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod httpMethod, string uri, string optionalPayload, string jwt)
        {
            return await SendRequestAsync(httpMethod, uri, optionalPayload, 10, jwt);
        }

        public static async Task<HttpResponseMessage> SendRequestAsync(HttpMethod httpMethod, string uri, string optionalPayload, int timeout, string jwt = null, bool isOnlineCheck = false)
        {

            if (!SoftwareCoUtil.isTelemetryOn())
            {
                return null;
            }

            if (!isOnlineCheck && !SoftwareUserSession.isOnline)
            {
                return null;
            }

            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(timeout)
            };
            var cts = new CancellationTokenSource();
            HttpResponseMessage response = null;
            if (jwt == null)
            {
                object jwtObj = null;
                  jwtObj  = SoftwareUserSession.GetJwt();
                if (jwtObj != null)
                {
                    jwt = (string)jwtObj;
                }
            }
            if (jwt != null)
            {
                // add the authorizationn
                client.DefaultRequestHeaders.Add("Authorization", jwt);
            }
            HttpContent contentPost = null;
            try
            {
                if (optionalPayload != null)
                {
                    contentPost = new StringContent(optionalPayload, Encoding.UTF8, "application/json");
                }
            } catch (Exception e)
            {
                NotifyPostException(e);
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
    }

    public class SpotifyToken
    {
        public string Access_token { get; set; }
        public string Token_type { get; set; }
        public double Expires_in { get; set; }
        public DateTime Expire_date { get; set; }
    }
}
