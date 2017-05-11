using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    public class HttpHelper
    {
        public static async Task<ResponseEx> SendRequest(HttpMethod method, Uri url, string postData, AcceptHeader acceptHeader, ContentType contentType, Uri referrer, string cookies)
        {
            try
            {
                var responseEx = new ResponseEx { Content = string.Empty, ContentLength = 0 };
                var requestMessage = GetHttpRequestMessage(method, postData, acceptHeader, contentType, referrer, cookies);

                var client = GetHttpClient(url, "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:53.0) Gecko/20100101 Firefox/53.0");

                if (client != null)
                {
                    var response = await client.SendAsync(requestMessage);
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    responseEx.ResponseMessage = response;
                    IEnumerable<string> values;
                    var contentLength = 0;

                    // Save cookies

                    if (response.Content.Headers.TryGetValues("Content-Length", out values))
                    {
                        contentLength = Convert.ToInt32(values.ElementAt(0));
                    }

                    if (response.StatusCode == HttpStatusCode.Redirect)
                    {
                        if (response.Headers.TryGetValues("Set-Cookie", out values))
                        {
                            responseEx.Cookies = string.Join(";", values);
                        }
                        if (response.Headers.TryGetValues("Location", out values))
                        {
                            responseEx.RedirectUrl = values.First();
                        }
                    }
                    else
                    {
                        var reader = new StreamReader(responseStream, Encoding.UTF8);
                        responseEx.Content = reader.ReadToEnd();
                        responseEx.ContentLength = contentLength;
                    }

                    return responseEx;
                }

                return null;
            }
            catch (HttpRequestException /*exc*/)
            {
                return null;
            }
        }

        private static HttpRequestMessage GetHttpRequestMessage(HttpMethod method, string postData, AcceptHeader acceptHeader,
            ContentType contentType, Uri referrer, string cookies)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage { Method = method };

            // uncomment if you want to recieve gzipped response
            //requestMessage.Headers.AcceptEncoding.TryParseAdd("gzip,deflate");
            requestMessage.Headers.Accept.TryParseAdd(GetEnumDescription(acceptHeader));
            requestMessage.Headers.Host = "app.pluralsight.com";
            requestMessage.Headers.Referrer = referrer;

            if (!string.IsNullOrEmpty(cookies))
            {
                requestMessage.Headers.Add("Cookie", cookies);
            }

            if (method == HttpMethod.Post)
            {
                requestMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(postData)));
                requestMessage.Content.Headers.Add("Content-Type", GetEnumDescription(contentType));
            }
            return requestMessage;
        }

        private static string GetEnumDescription(Enum value)
        {
            var fi = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes.Any())
            {
                return attributes[0].Description;
            }
            return value.ToString();
        }

        private static HttpClient GetHttpClient(Uri url, string userAgent)
        {
            try
            {
                HttpClient httpClient = new HttpClient(new HttpClientHandler
                {
                    AllowAutoRedirect = false,
                    UseCookies = false
                })
                { BaseAddress = url };

                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
                httpClient.DefaultRequestHeaders.Add("User-agent", userAgent);
                return httpClient;
            }
            catch (Exception /*exc*/)
            {
                return null;
            }
        }
    }
}
