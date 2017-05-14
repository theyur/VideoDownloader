﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    public class HttpHelper
    {
        public string Cookies { get; set; }

        public Uri Referrer { get; set; }

        public string AcceptEncoding { get; set; }

        public AcceptHeader AcceptHeader { get; set; }

        public ContentType ContentType { get; set; }

        public string UserAgent { get; set; }

        public async Task DownloadWithProgressAsync(Uri fileUri, string fileName, IProgress<FileDownloadingProgressArguments> downloadingProgress, CancellationToken token)
        {
            var responseBuffer = new byte[4096];
            var fullFileNameWithoutExtension = $@"{Path.GetDirectoryName(fileName)}\{Path.GetFileNameWithoutExtension(fileName)}";
            var extension = Path.GetExtension(fileName);
            var httpClient = GetHttpClient(fileUri, UserAgent);
            using (var request = new HttpRequestMessage(HttpMethod.Get, fileUri))
            {
                var httpReponseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                using (var contentStream = await httpReponseMessage.Content.ReadAsStreamAsync())
                using (
                    Stream stream = new FileStream($"{fullFileNameWithoutExtension}.part", FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    int bytesRead;
                    int totalBytesRead = 0;
                    do
                    {
                        bytesRead = await contentStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, token);
                        totalBytesRead += bytesRead;
                        await stream.WriteAsync(responseBuffer, 0, bytesRead, token);

                        downloadingProgress?.Report(new FileDownloadingProgressArguments
                        {
                            Percentage = httpReponseMessage.Content.Headers.ContentLength != 0 ? 
                            (int)(((double) totalBytesRead) / httpReponseMessage.Content.Headers.ContentLength * 100)
                                : -1,
                            FileName = $"{fullFileNameWithoutExtension}{extension}"
                        });
                    } while (bytesRead > 0);
                }
            }
            File.Move($"{fullFileNameWithoutExtension}.part", $"{fullFileNameWithoutExtension}{extension}");
        }
        public async Task<ResponseEx> SendRequest(HttpMethod method, Uri url, string postData, CancellationToken cancellationToken)
        {
            try
            {
                var responseEx = new ResponseEx { Content = string.Empty, ContentLength = 0 };
                var requestMessage = GetHttpRequestMessage(method, postData);

                var client = GetHttpClient(url, UserAgent);

                if (client != null)
                {
                    using (var response = await client.SendAsync(requestMessage, cancellationToken))
                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
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

        private HttpRequestMessage GetHttpRequestMessage(HttpMethod method, string postData)
        {
            HttpRequestMessage requestMessage = new HttpRequestMessage { Method = method };

            // uncomment if you want to recieve gzipped response
            if (AcceptEncoding != string.Empty)
            {
                requestMessage.Headers.AcceptEncoding.TryParseAdd(AcceptEncoding);
            }
            requestMessage.Headers.Accept.TryParseAdd(GetEnumDescription(AcceptHeader));
            requestMessage.Headers.Host = "app.pluralsight.com";
            requestMessage.Headers.Referrer = Referrer;

            if (!string.IsNullOrEmpty(Cookies))
            {
                requestMessage.Headers.Add("Cookie", Cookies);
            }

            if (method == HttpMethod.Post)
            {
                requestMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(postData)));
                requestMessage.Content.Headers.Add("Content-Type", GetEnumDescription(ContentType));
            }
            return requestMessage;
        }

        private static string GetEnumDescription(Enum value)
        {
            var field = value.GetType().GetField(value.ToString());

            var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes.Any() ? attributes[0].Description : value.ToString();
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
