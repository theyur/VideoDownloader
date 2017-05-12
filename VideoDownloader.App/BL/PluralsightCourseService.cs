using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    class PluralsightCourseService : ICourseService
    {
        #region Fields

        readonly IConfigProvider _configProvider;
        private readonly object _syncObj = new object();
        private readonly string _userAgent;
        #endregion

        #region Constructors
        public PluralsightCourseService(IConfigProvider configProvider)
        {
            _configProvider = configProvider;
            _userAgent = _configProvider.UserAgent;
        }

        #endregion

        #region Properties

        public string Cookies { get; set; }
        public Dictionary<string, List<CourseDescription>> CoursesByToolName { get; set; } = new Dictionary<string, List<CourseDescription>>();
        public string CachedProductsJson { get; set; }

        #endregion

        private int GenerateRandomNumber(int min, int max)
        {
            lock (_syncObj)
            {
                var random = new Random();
                return random.Next(min, max);
            }
        }

        string GetValidPath(string path)
        {
            var regexSearch = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());
            var r = new Regex($"[{Regex.Escape(regexSearch)}]");
            path = r.Replace(path, "");
            return path;
        }

        public async Task DownloadAsync(string productId,
            IProgress<CourseDownloadingProgressArguments> downloadingProgress,
            IProgress<int> timeoutProgress,
            CancellationToken token)
        {
            var destinationFolder = _configProvider.DownloadsPath;

            var rpcUri = "https://app.pluralsight.com/player/functions/rpc";
            var chunkSize = 4096;

            RpcData rpcData = await GetDeserialisedRpcData(rpcUri, productId);

            var course = rpcData.Payload.Course;

            DownloadCourse(course, downloadingProgress, timeoutProgress, token);
        }

        private void DownloadCourse(CourseRpc course, IProgress<CourseDownloadingProgressArguments> downloadingProgress, IProgress<int> timeoutProgress, CancellationToken token)
        {
            string destinationFolder = "D:";
            var partsNumber = course.Modules.Sum(module => module.Clips.Count());

            using (var httpClient = new HttpClient())
            {
                System.Timers.Timer timer = null;

                var courseDirectory = CreateCourseDirectory(destinationFolder, course.Title);
                try
                {
                    var moduleCounter = 0;
                    foreach (var module in course.Modules)
                    {
                        ++moduleCounter;
                        var moduleDirectory = CreateModuleDirectory(courseDirectory, moduleCounter, module.Title);

                        var clipCounter = 0;
                        foreach (var clip in module.Clips)
                        {
                            token.ThrowIfCancellationRequested();

                            var timeout = GenerateRandomNumber(_configProvider.MinTimeout, _configProvider.MaxTimeout);
                            timer = new System.Timers.Timer(1000);
                            timer.Elapsed += (sender, e) =>
                            {
                                if (timeout > 0)
                                {
                                    timeoutProgress.Report(--timeout);
                                }
                            };

                            ++clipCounter;
                            var fileNameWithoutExtension = $@"{moduleDirectory}\{clipCounter:00}.{GetValidPath(clip.Title)}";
                            var fileName = $"{fileNameWithoutExtension}.part";
                            if (File.Exists($"{fileNameWithoutExtension}.mp4"))
                            {
                                continue;
                            }

                            if (File.Exists($"{fileNameWithoutExtension}.part"))
                            {
                                File.Delete($"{fileNameWithoutExtension}.part");
                            }

                            				var progressValue = (int) (((double) clipCounter)/partsNumber*100);
                            				var postJson = CreateFilePostJson(clip, "1280x720");
                            //				var urlString = "https://app.pluralsight.com/player/retrieve-url";
                            //				var clipResponse =
                            //					await
                            //						SendRequest(HttpMethod.Post, new Uri(urlString), postJson, AcceptHeader.JsonTextPlain,
                            //							ContentType.AppJsonUtf8);
                            //				dynamic obj = JObject.Parse(clipResponse.Content);
                            //				if (obj.status == HttpStatusCode.BadRequest || obj.status == HttpStatusCode.NotFound)
                            //				{
                            //					postJson = CreateFilePostJson(clip, "1024x768");
                            //					urlString = "https://app.pluralsight.com/player/retrieve-url";
                            //					clipResponse =
                            //						await
                            //							SendRequest(HttpMethod.Post, new Uri(urlString), postJson, AcceptHeader.JsonTextPlain,
                            //								ContentType.AppJsonUtf8);
                            //				}
                            //				var clipFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ClipFile>(clipResponse.Content);
                            //				Uri fileUri = new Uri(clipFile.Urls[0].Url);
                            //				//var clipFileResponse =
                            //				//	await
                            //				//		CreateRequest(HttpMethod.Head, fileUri, null, AcceptHeader.HtmlXml,
                            //				//			ContentType.AppXWwwFormUrlencode, fileUri.Host);

                            //				var initialProgressArgs = new CourseDownloadingProgressArguments
                            //				{
                            //					CurrentAction = "Downloading",
                            //					CourseName = course.Title,
                            //					ClipName = fileName,
                            //					CourseProgress = progressValue,
                            //					ClipProgress = 100
                            //				};

                            //				downloadingProgress.Report(initialProgressArgs);

                            //				var responseBuffer = new byte[chunkSize];
                            //				using (var request = new HttpRequestMessage(HttpMethod.Get, fileUri))
                            //				{
                            //					var httpReponseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
                            //					using (var contentStream = await httpReponseMessage.Content.ReadAsStreamAsync())
                            //					{
                            //						using (
                            //							Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, chunkSize, true))
                            //						{
                            //							int bytesRead;
                            //							int totalBytesRead = 0;
                            //							do
                            //							{
                            //								bytesRead = await contentStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, token);
                            //								totalBytesRead += bytesRead;
                            //								stream.Write(responseBuffer, 0, bytesRead);

                            //								var progressArgs = new CourseDownloadingProgressArguments
                            //								{
                            //									CurrentAction = "Downloading",
                            //									CourseName = course.Title,
                            //									ClipName = fileName,
                            //									CourseProgress = progressValue,
                            //									ClipProgress = httpReponseMessage.Content.Headers.ContentLength != 0 ? (int) (((double) totalBytesRead)/ httpReponseMessage.Content.Headers.ContentLength*100): -1
                            //								};

                            //								downloadingProgress.Report(progressArgs);
                            //							} while (bytesRead > 0);
                            //						}
                            //					}
                            //				}

                            //				timer.Enabled = true;
                            //				File.Move($"{fileNameWithoutExtension}.part", $"{fileNameWithoutExtension}.mp4");
                            //				await Task.Delay(timeout*1000, token);
                            //				timer.Enabled = false;
                            //				timeoutProgress.Report(0);
                        }
                    }
                }
                catch (OperationCanceledException /*ex*/)
                {

                }
                finally
                {
                    var progressArgs = new CourseDownloadingProgressArguments
                    {
                        CourseName = course.Title,
                        ClipName = string.Empty,
                        CourseProgress = 0,
                        ClipProgress = 0
                    };

                    downloadingProgress.Report(progressArgs);
                    if (timer != null)
                    {
                        timer.Enabled = false;
                    }
                    timeoutProgress.Report(0);
                }
            }
        }

        private string CreateModuleDirectory(string courseDirectory, int moduleCounter, string moduleTitle)
        {
            var moduleDirectory = $@"{courseDirectory}\{moduleCounter:00}.{GetValidPath(moduleTitle)}";
            Directory.CreateDirectory(moduleDirectory);
            return moduleDirectory;
        }

        private string CreateCourseDirectory(string destinationFolder, string courseTitle)
        {
            var courseDirectory = $@"{destinationFolder}\{GetValidPath(courseTitle)}";
            Directory.CreateDirectory(courseDirectory);
            return courseDirectory;

        }

        private string BuildViewclipData()
        {
            ViewclipData viewclipData = new ViewclipData();
            return viewclipData.ToString();
        }

        private async Task<RpcData> GetDeserialisedRpcData(string rpcUri, string productId)
        {
            string rpcJson = $"{{\"fn\":\"bootstrapPlayer\", \"payload\":{{\"courseId\":\"{productId}\"}} }}";

            var courseRespone = await HttpHelper.SendRequest(HttpMethod.Post, new Uri(rpcUri), rpcJson,
                AcceptHeader.JsonTextPlain, ContentType.AppJsonUtf8, new Uri("https://www.pluralsight.com"),
                Cookies);

            var courseJson = courseRespone.Content;
            return Newtonsoft.Json.JsonConvert.DeserializeObject<RpcData>(courseJson);
        }

        public async Task<bool> DownloadProductsJsonAsync()
        {
            try
            {
                var productsJsonResponse = await HttpHelper.SendRequest(HttpMethod.Get,
                    new Uri(
                        "https://app.pluralsight.com/search/proxy?i=1&q1=course&x1=categories&m_Sort=updated_date&count=7174"),
                    null, AcceptHeader.HtmlXml,
                    ContentType.AppXWwwFormUrlencode,
                    new Uri("https://www.pluralsight.com"),
                    Cookies);
                CachedProductsJson = productsJsonResponse.Content;
                ProcessResult();
                //CachedProductsJson = await Task.Run<string>(() => { return File.ReadAllText("D:\\json.txt"); });
                return !string.IsNullOrEmpty(CachedProductsJson);
            }
            catch (Exception exc)
            {
                return false;
            }
        }


        private void ProcessResult()
        {
            CoursesByToolName.Clear();
            var allProducts = Newtonsoft.Json.JsonConvert.DeserializeObject<AllProducts>(CachedProductsJson);
            foreach (var product in allProducts.ResultSets[0].Results)
            {
                try
                {
                    var tools = product.Tools?.Split('|') ?? new[] { "No category" };
                    foreach (var tool in tools)
                    {
                        if (!CoursesByToolName.ContainsKey(tool))
                        {
                            CoursesByToolName[tool] = new List<CourseDescription>();
                        }
                        CoursesByToolName[tool].Add(product);
                    }
                }
                catch (Exception exc)
                {
                    return;
                }
            }
        }

        public async Task<bool> ReadFromFileProductsJsonAsync(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    CachedProductsJson = await Task.Run<string>(() => { return File.ReadAllText("D:\\json.txt"); });
                }
                else
                {
                    await DownloadProductsJsonAsync();
                }

                return !string.IsNullOrEmpty(CachedProductsJson);
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        public async Task<List<CourseDescription>> GetToolCourses(string toolName)
        {
            List<CourseDescription> courses = await Task.Run(() => CoursesByToolName.Single(kvp => kvp.Key == toolName).Value);
            return courses;
        }
        string CreateFilePostJson(Clip clip, string clipQuality)
        {
            var stringParts = clip.ModuleTitle.Split('&');
            var dict = stringParts.ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1]);
            string[] nameParts = dict["name"].Split('|');
            string name = nameParts.Length < 2 ? nameParts[0] : nameParts[1];
            var o = JObject.FromObject(new
            {
                author = dict["author"],
                moduleName = name,
                courseName = dict.ElementAt(0).Value,
                clipIndex = Convert.ToInt32(dict["clip"]),
                mediaType = "mp4",
                quality = clipQuality,
                includeCaptions = false,
                locale = "en"
            });
            return o.ToString();
        }
    }
}
