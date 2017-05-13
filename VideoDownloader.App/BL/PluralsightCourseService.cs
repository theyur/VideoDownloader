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
using System.Timers;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;
using Timer = System.Timers.Timer;

namespace VideoDownloader.App.BL
{
    class PluralsightCourseService : ICourseService
    {
        #region Fields

        private readonly IConfigProvider _configProvider;
        private readonly object _syncObj = new object();
        private readonly string _userAgent;
        private CancellationToken _token;
        private IProgress<CourseDownloadingProgressArguments> _clipDownloadingProgress;
        private Progress<FileDownloadingProgressArguments> _downloadingProgress;
        const int ChunkSize = 4096;
        private int _totalCourseDownloadingProgess = 0;
        private int _timeout;
        private readonly Timer _timer = new System.Timers.Timer(1000);
        private IProgress<int> _timeoutProgress;

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
            _timeoutProgress = timeoutProgress;
            _clipDownloadingProgress = downloadingProgress;
            var destinationFolder = _configProvider.DownloadsPath;
            _token = token;
            var rpcUri = "https://app.pluralsight.com/player/functions/rpc";

            RpcData rpcData = await GetDeserialisedRpcData(rpcUri, productId);

            await DownloadCourse(rpcData);
        }

        private async Task DownloadCourse(RpcData rpcData)
        {
            string destinationFolder = _configProvider.DownloadsPath;
            
            var course = rpcData.Payload.Course;
            _timer.Elapsed += OnTimerElapsed;
          
            var courseDirectory = CreateCourseDirectory(destinationFolder, course.Title);
            try
            {
                var moduleCounter = 0;
                foreach (var module in course.Modules)
                {
                    ++moduleCounter;
                    await DownloadModule(rpcData, courseDirectory, moduleCounter, module);
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

                _clipDownloadingProgress.Report(progressArgs);
                if (_timer != null)
                {
                    _timer.Enabled = false;
                }
                _timeoutProgress.Report(0);
            }
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (_timeout > 0)
            {
                _timeoutProgress.Report(--_timeout);
            }
        }

        private async Task DownloadModule(RpcData rpcData,
            string courseDirectory,
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
                    rpcData.Payload.Course.Modules.Sum(m => m.Clips.Length));
            }
        }

        private async Task DownloadClip(Uri clipUrl, string fileNameWithoutExtension, int clipCounter, int partsNumber)
        {
            _token.ThrowIfCancellationRequested();

            if (File.Exists($"{fileNameWithoutExtension}.mp4")) return;

            RemovePartiallyDownloadedFile(fileNameWithoutExtension);

            var fileName = $"{fileNameWithoutExtension}.part";
            _totalCourseDownloadingProgess = (int)(((double)clipCounter) / partsNumber * 100);

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
                CourseProgress = _totalCourseDownloadingProgess,
                ClipProgress = 100
            };

            _downloadingProgress = new Progress<FileDownloadingProgressArguments>();
            _downloadingProgress.ProgressChanged += OnProgressChanged;

            await httpHelper.DownloadWithProgressAsync(clipUrl, $"{fileNameWithoutExtension}.mp4", _downloadingProgress, _token);

            _timeout = GenerateRandomNumber(_configProvider.MinTimeout, _configProvider.MaxTimeout);

            _timer.Enabled = true;
            await Task.Delay(_timeout * 1000, _token);
            _timer.Enabled = false;
            _timeoutProgress.Report(0);
            _downloadingProgress.ProgressChanged -= OnProgressChanged;
            

        }

        private void OnProgressChanged(object sender, FileDownloadingProgressArguments e)
        {
            var progressArgs = new CourseDownloadingProgressArguments
            {
                CurrentAction = "Downloading",
                //CourseName = course.Title,
                ClipName = e.FileName,
                CourseProgress = _totalCourseDownloadingProgess,
                ClipProgress = e.Percentage
            };
            _clipDownloadingProgress.Report(progressArgs);
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
            var httpHelper = new HttpHelper
            {
                AcceptHeader = AcceptHeader.JsonTextPlain,
                AcceptEncoding = "",
                ContentType = ContentType.AppJsonUtf8,
                Cookies = Cookies,
                Referrer = new Uri("https://www.pluralsight.com")
            };
            var courseRespone = await httpHelper.SendRequest(HttpMethod.Post, new Uri(rpcUri), rpcJson, _token);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<RpcData>(courseRespone.Content);
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
                    new Uri("https://app.pluralsight.com/search/proxy?i=1&q1=course&x1=categories&m_Sort=updated_date&count=7010"),
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
                    CachedProductsJson = await Task.Run(() => File.ReadAllText("D:\\json.txt"), _token);
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
            var courses = await Task.Run(() => CoursesByToolName.Single(kvp => kvp.Key == toolName).Value, _token);
            return courses;
        }
    }
}
