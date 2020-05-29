using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace BlockBase.Utils
{
    public static class HttpHelper
    {
        public static HttpWebRequest ComposeWebRequestPost(string url) => ComposeWebRequest(url, "POST");
        public static HttpWebRequest ComposeWebRequestGet(string url) => ComposeWebRequest(url, "GET");

        public static HttpWebRequest ComposeWebRequest(string url, string method)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = method;
            request.ContentType = "application/json";

            return request;
        }

        public static async Task<string> CallWebRequest(HttpWebRequest httpWebRequest, object jsonBody)
        {
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonBody);

                await streamWriter.WriteAsync(json);
            }

            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static async Task<string> CallWebRequestNoSSLVerification(HttpWebRequest httpWebRequest, object jsonBody)
        {
            httpWebRequest.ServerCertificateValidationCallback = delegate {return true;};
            
            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonBody);

                await streamWriter.WriteAsync(json);
            }

            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        public static async Task<string> CallWebRequestNoSSLVerification(HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ServerCertificateValidationCallback = delegate {return true;};
            
            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
        }

        
        public static async Task<string> CallWebRequest(HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ServerCertificateValidationCallback = delegate {return true;};
            
            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
        }

    }
}