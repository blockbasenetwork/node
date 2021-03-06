using System;
using System.Diagnostics;
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
            httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };

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
            httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };

            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
        }


        public static async Task<string> CallWebRequest(HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };

            var response = (HttpWebResponse)httpWebRequest.GetResponse();

            return await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
        }

        public static async Task<(string, string)> MeasureWebRequest(string endpoint, HttpWebRequest httpWebRequest)
        {
            try
            {
                httpWebRequest.ServerCertificateValidationCallback = delegate { return true; };
                httpWebRequest.Timeout = 5000;

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                var response = (HttpWebResponse)httpWebRequest.GetResponse();
                var streamReader = await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
                stopwatch.Stop();

                return (endpoint, stopwatch.ElapsedMilliseconds.ToString());
            }
            catch (Exception)
            {
                return (endpoint, "No response within 5 seconds");
            }
        }
    }
}