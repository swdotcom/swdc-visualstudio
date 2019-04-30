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

        public static async Task GetSpotifyTrackInfoAsync(LocalSpotifyTrackInfo trackInfo)
        {
            string webResponse = string.Empty;
            try
            {
                HttpClient client = new HttpClient();

                string searchQuery = string.Format("artist:{0} track:{1}", trackInfo.artist, trackInfo.name);
                searchQuery = WebUtility.UrlEncode(searchQuery);

                string spotifyUrl = string.Format("https://api.spotify.com/v1/search?q={0}&type=track&limit=2&offset=0", searchQuery);
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(spotifyUrl);
                webRequest.Method = "GET";
                webRequest.ContentType = "application/json";
                webRequest.Accept = "application/json";
                webRequest.Headers.Add("Authorization: Bearer " + token.Access_token);

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
                jsonObj.TryGetValue("tracks", out object trackObj);
                IDictionary<string, object> trackDict = (trackObj == null) ? null : (IDictionary<string, object>)trackObj;
                JsonArray items = null;
                if (trackDict != null)
                {
                    trackDict.TryGetValue("items", out object itemsObj);
                    items = (itemsObj == null) ? null : (JsonArray)itemsObj;
                }

                // need: id, name (already have it), artist (already have it), state, duration
                // type = "spotify"
                // other attributes to send: genre, start, end
                if (items != null && items.Count > 0)
                {
                    IDictionary<string, object> trackData = (IDictionary<string, object>)items[0];
                    trackData.TryGetValue("uri", out object spotifyIdObj);
                    string spotifyId = (spotifyIdObj == null) ? null : Convert.ToString(spotifyIdObj);
                    trackData.TryGetValue("duration_ms", out object durationMsObj);
                    long durationSec = (durationMsObj == null) ? 0L : Convert.ToInt64(durationMsObj);

                    trackInfo.duration = durationSec;
                    trackInfo.type = "spotify";
                    trackInfo.state = "playing";
                    trackInfo.genre = "";
                    trackInfo.id = spotifyId;
                    trackInfo.start = SoftwareCoUtil.getNowInSeconds();
                    double offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now).TotalMinutes;
                    trackInfo.local_start = trackInfo.start + ((int)offset * 60);
                }
            }
            catch (Exception tex)
            {
                Logger.Error("error: " + tex.Message);
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
                object jwtObj = SoftwareUserSession.GetJwt();
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
