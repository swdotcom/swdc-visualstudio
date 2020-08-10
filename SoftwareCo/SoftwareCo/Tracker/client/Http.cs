using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SoftwareCo
{
    public class Http
    {

        static string swdc_endpoint = "";

        public static void Initialize(string endopint)
        {
            swdc_endpoint = endopint;
        }

        public static async Task<Response> GetAsync(string api)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    if (CacheManager.HasJwt())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", CacheManager.jwt);
                    }

                    // create the url
                    string url = $"{Constants.api_endpoint}{api}";

                    // make the GET call
                    HttpResponseMessage resp = await client.GetAsync(url);
                    resp.EnsureSuccessStatusCode();

                    return await BuildResponse(resp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SwdcVsTracker - GET request error: {0}", e.Message);
            }
            return new Response();
        }

        public static async Task<Response> PostAsync(string api, object payload)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);
                    if (CacheManager.HasJwt())
                    {
                        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", CacheManager.jwt);
                    }

                    // create the url
                    string url = $"{Constants.api_endpoint}{api}";

                    // make the POST call
                    string payloadStr = JsonConvert.SerializeObject(payload);
                    HttpContent content = new StringContent(payloadStr, Encoding.UTF8, "application/json");
                    HttpResponseMessage resp = await client.PostAsync(url, content);
                    resp.EnsureSuccessStatusCode();

                    return await BuildResponse(resp);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("SwdcVsTracker - POST request error: {0}", e.Message);
            }
            return new Response();
        }

        private static async Task<Response> BuildResponse(HttpResponseMessage resp)
        {
            Response httpResp = new Response();
            string jsonString = await resp.Content.ReadAsStringAsync();
            httpResp.ok = resp.IsSuccessStatusCode;
            httpResp.responseData = JsonConvert.DeserializeObject<object>(jsonString);
            return httpResp;
        }
    }


}
