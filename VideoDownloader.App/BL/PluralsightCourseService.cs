using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;
using Timer = System.Timers.Timer;

namespace VideoDownloader.App.BL
{
    class PluralsightCourseService : ICourseService
    {
        #region Fields

        private readonly Timer _timer = new System.Timers.Timer(1000);
        private readonly IConfigProvider _configProvider;
        private readonly object _syncObj = new object();
        private readonly string _userAgent;
        private CancellationToken _token;
        private IProgress<CourseDownloadingProgressArguments> _downloadingProgress;
        const int ChunkSize = 4096;

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
            _token = token;
            var rpcUri = "https://app.pluralsight.com/player/functions/rpc";

            RpcData rpcData = await GetDeserialisedRpcData(rpcUri, productId);

            await DownloadCourse(rpcData, downloadingProgress, timeoutProgress);
        }

        private async Task DownloadCourse(RpcData rpcData, IProgress<CourseDownloadingProgressArguments> downloadingProgress, IProgress<int> timeoutProgress)
        {
            string destinationFolder = _configProvider.DownloadsPath;
            _downloadingProgress = downloadingProgress;
            var course = rpcData.Payload.Course;

            var courseDirectory = CreateCourseDirectory(destinationFolder, course.Title);
            try
            {
                var moduleCounter = 0;
                foreach (var module in course.Modules)
                {
                    ++moduleCounter;
                    await DownloadModule(rpcData, timeoutProgress, courseDirectory, moduleCounter, module);
                }
            }
            catch (OperationCanceledException /*ex*/)
            {

            }
            finally
            {
                var progressArgs = new CourseDownloadingProgressArguments
                {
                    //CourseName = course.Title,
                    ClipName = string.Empty,
                    CourseProgress = 0,
                    ClipProgress = 0
                };

                downloadingProgress.Report(progressArgs);
                if (_timer != null)
                {
                    _timer.Enabled = false;
                }
                timeoutProgress.Report(0);
            }
        }

        private async Task DownloadModule(RpcData rpcData,
            IProgress<int> timeoutProgress, string courseDirectory,
            int moduleCounter, Module module)
        {
            var moduleDirectory = CreateModuleDirectory(courseDirectory, moduleCounter, module.Title);
            var course = rpcData.Payload.Course;
            var clipCounter = 0;
            string referrer =
                   $"https://app.pluralsight.com/player?course={course.Name}&author={module.Author}&name={module.Name}&clip={clipCounter - 1}&mode=live";

            HttpHelper httpHelper = new HttpHelper
            {
                AcceptEncoding = "",
                AcceptHeader = AcceptHeader.All,
                ContentType = ContentType.AppJsonUtf8,
                Cookies = Cookies,
                Referrer = new Uri(referrer)
            };

            foreach (var clip in module.Clips)
            {
                ++clipCounter;
                var postJson = BuildViewclipData(rpcData, moduleCounter, clipCounter);

                var fileName = GetFullFileNameWithoutExtension(clipCounter, moduleDirectory, clip);

                var viewclipResonse = await httpHelper.SendRequest(HttpMethod.Post,
                    new Uri("https://app.pluralsight.com/video/clips/viewclip"),
                    postJson,
                    _token);

                var clipFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ClipFile>(viewclipResonse.Content);
                await DownloadClip(new Uri(clipFile.Urls[1].Url),
                    fileName,
                    clipCounter,
                    rpcData.Payload.Course.Modules.Sum(m => m.Clips.Length),
                    timeoutProgress);
            }
        }

        private async Task DownloadClip(Uri clipUrl, string fileNameWithoutExtension, int clipCounter, int partsNumber, IProgress<int> timeoutProgress)
        {
            _token.ThrowIfCancellationRequested();

            if (File.Exists($"{fileNameWithoutExtension}.mp4")) return;

            RemovePartiallyDownloadedFile(fileNameWithoutExtension);

            var fileName = $"{fileNameWithoutExtension}.part";
            var progressValue = (int)(((double)clipCounter) / partsNumber * 100);

            var httpHelper = new HttpHelper
            {
                AcceptEncoding = "",
                AcceptHeader = AcceptHeader.All,
                ContentType = ContentType.Video,
                Cookies = Cookies,
                Referrer = new Uri("http://vid20.pluralsight.com")
            };

            var clipFileResponse =
                await
                    httpHelper.SendRequest(HttpMethod.Head, clipUrl, null, _token);

            var initialProgressArgs = new CourseDownloadingProgressArguments
            {
                CurrentAction = "Downloading",
                //CourseName = course.Title,
                ClipName = fileName,
                CourseProgress = progressValue,
                ClipProgress = 100
            };

            _downloadingProgress.Report(initialProgressArgs);

            var responseBuffer = new byte[ChunkSize];
            using (var request = new HttpRequestMessage(HttpMethod.Get, fileUri))
            {
                var httpReponseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _token);
                using (var contentStream = await httpReponseMessage.Content.ReadAsStreamAsync())
                {
                    using (
                        Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, ChunkSize, true))
                    {
                        int bytesRead;
                        int totalBytesRead = 0;
                        do
                        {
                            bytesRead = await contentStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, _token);
                            totalBytesRead += bytesRead;
                            stream.Write(responseBuffer, 0, bytesRead);

                            var progressArgs = new CourseDownloadingProgressArguments
                            {
                                CurrentAction = "Downloading",
                                CourseName = course.Title,
                                ClipName = fileName,
                                CourseProgress = progressValue,
                                ClipProgress = httpReponseMessage.Content.Headers.ContentLength != 0 ? (int)(((double)totalBytesRead) / httpReponseMessage.Content.Headers.ContentLength * 100) : -1
                            };

                            downloadingProgress.Report(progressArgs);
                        } while (bytesRead > 0);
                    }
                }
            }

            //timer.Enabled = true;
            File.Move($"{fileNameWithoutExtension}.part", $"{fileNameWithoutExtension}.mp4");
            //await Task.Delay(timeout * 1000, token);
            //timer.Enabled = false;
            timeoutProgress.Report(0);
        }

        private static void RemovePartiallyDownloadedFile(string fileNameWithoutExtension)
        {
            if (File.Exists($"{fileNameWithoutExtension}.part"))
            {
                File.Delete($"{fileNameWithoutExtension}.part");
            }
        }

        private string GetFullFileNameWithoutExtension(int clipCounter, string moduleDirectory, Clip clip)
        {
            return $@"{moduleDirectory}\{clipCounter:00}.{GetValidPath(clip.Title)}";
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

        private string BuildViewclipData(RpcData rpcData, int moduleCounter, int clipCounter)
        {
            ViewclipData viewclipData = new ViewclipData();
            Module module = rpcData.Payload.Course.Modules[moduleCounter - 1];
            Clip clip = module.Clips[clipCounter - 1];
            viewclipData.Author = module.Author;
            viewclipData.IncludeCaptions = rpcData.Payload.Course.CourseHasCaptions;
            viewclipData.ClipIndex = clipCounter - 1;
            viewclipData.CourseName = rpcData.Payload.Course.Name;
            viewclipData.Locale = "en";
            viewclipData.ModuleName = module.Name;
            viewclipData.MediaType = "mp4";
            viewclipData.Quality = rpcData.Payload.Course.SupportsWideScreenVideoFormats ? "1280x720" : "1024x768";
            return Newtonsoft.Json.JsonConvert.SerializeObject(viewclipData);
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
                var httpHelper = new HttpHelper
                {
                    AcceptEncoding = "",
                    AcceptHeader = AcceptHeader.HtmlXml,
                    ContentType = ContentType.AppXWwwFormUrlencode,
                    Cookies = Cookies,
                    Referrer = new Uri("https://www.pluralsight.com")
                };
                var productsJsonResponse = await httpHelper.SendRequest(HttpMethod.Get,
                    new Uri(
                        "https://app.pluralsight.com/search/proxy?i=1&q1=course&x1=categories&m_Sort=updated_date&count=7010"),
                    null, _token);

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
