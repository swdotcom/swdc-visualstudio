using System;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Threading;

namespace SoftwareCo
{
    class SoftwareHttpManager
    {
        public static bool IsOk(HttpResponseMessage response)
        {
            return (response != null && response.StatusCode == HttpStatusCode.OK);
        }

        public static async Task<HttpResponseMessage> SendDashboardRequestAsync(HttpMethod httpMethod, string uri)
        {
            return await SendRequestAsync(httpMethod, uri, null);
        }

        public static async Task<HttpResponseMessage> SendRequestAsync
            (HttpMethod httpMethod, string uri, string optionalPayload = null, string jwt = null, bool useJwt = true)
        {

            if (!SoftwareCoUtil.isTelemetryOn())
            {
                return null;
            }

            if (!SoftwareUserManager.isOnline)
            {
                return null;
            }

            HttpClient client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            var cts = new CancellationTokenSource();
            HttpResponseMessage response = null;
            if (jwt == null && useJwt)
            {
                jwt = FileManager.getItemAsString("jwt");
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
