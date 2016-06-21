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

		string _cookies;
		readonly IConfigProvider _configProvider;
		private readonly object _syncObj = new object();
		private bool _loggedIn;
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

		public string CachedProductsJson { get; set; }
		public string LoginResultJson { get; set; }

		#endregion

		private int GenerateRandomNumber(int min, int max)
		{
			lock (_syncObj)
			{
				var random = new Random(); // Or exception...
				return random.Next(min, max);
			}
		}

		public static string GetEnumDescription(Enum value)
		{
			var fi = value.GetType().GetField(value.ToString());

			var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (attributes.Any())
			{
				return attributes[0].Description;
			}
			return value.ToString();
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
			
			var productUri = $"{"https://app.pluralsight.com/learner/content/courses"}/{productId}";
			var chunkSize = 4096;

			// get json of all the parts of the course
			var courseRespone = await CreateRequest(HttpMethod.Get, new Uri(productUri), null, AcceptHeader.HtmlXml, ContentType.AppXWwwFormUrlencode, "app.pluralsight.com");
			var courseJson = courseRespone.Content;
			var course = Newtonsoft.Json.JsonConvert.DeserializeObject<Course>(courseJson);

			// count number of the parts 
			var partsNumber = course.Modules.Sum(module => module.Clips.Count());

			using (var httpClient = new HttpClient())
			{
				// creating course directory
				var courseDirectory = GetValidPath(course.Title);
				Directory.CreateDirectory($@"{destinationFolder}\{courseDirectory}");

				var moduleCounter = 0;
				System.Timers.Timer timer = null;

				try
				{
					foreach (var module in course.Modules)
					{
						++moduleCounter;

						// creating module directory
						var moduleDirectory = $@"{courseDirectory}\{moduleCounter:00}.{GetValidPath(module.Title)}";
						Directory.CreateDirectory($@"{destinationFolder}\{moduleDirectory}");

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
							var justFileName = $@"{destinationFolder}\{moduleDirectory}\{clipCounter:00}.{GetValidPath(clip.Title)}";
							var fileName = $"{justFileName}.part";
							if (File.Exists($"{justFileName}.mp4"))
							{
								continue;
							}

							if (File.Exists($"{justFileName}.part"))
							{
								File.Delete($"{justFileName}.part");
							}

							var progressValue = (int) (((double) clipCounter)/partsNumber*100);
							var postJson = CreateFilePostJson(clip, "1280x720");
							var urlString = "https://app.pluralsight.com/player/retrieve-url";
							var clipResponse =
								await
									CreateRequest(HttpMethod.Post, new Uri(urlString), postJson, AcceptHeader.JsonTextPlain,
										ContentType.AppJsonUtf8, "app.pluralsight.com");
							dynamic obj = JObject.Parse(clipResponse.Content);
							if (obj.status == HttpStatusCode.BadRequest)
							{
								postJson = CreateFilePostJson(clip, "1024x768");
								urlString = "https://app.pluralsight.com/player/retrieve-url";
								clipResponse =
									await
										CreateRequest(HttpMethod.Post, new Uri(urlString), postJson, AcceptHeader.JsonTextPlain,
											ContentType.AppJsonUtf8, "app.pluralsight.com");
							}
							var clipFile = Newtonsoft.Json.JsonConvert.DeserializeObject<ClipFile>(clipResponse.Content);
							Uri fileUri = new Uri(clipFile.Urls[0].Url);
							//var clipFileResponse =
							//	await
							//		CreateRequest(HttpMethod.Head, fileUri, null, AcceptHeader.HtmlXml,
							//			ContentType.AppXWwwFormUrlencode, fileUri.Host);
							
							var initialProgressArgs = new CourseDownloadingProgressArguments
							{
								CurrentAction = "Downloading",
								CourseName = course.Title,
								ClipName = fileName,
								CourseProgress = progressValue,
								ClipProgress = 100
							};

							downloadingProgress.Report(initialProgressArgs);

							var responseBuffer = new byte[chunkSize];
							using (var request = new HttpRequestMessage(HttpMethod.Get, fileUri))
							{
								var httpReponseMessage = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);
								using (var contentStream = await httpReponseMessage.Content.ReadAsStreamAsync())
								{
									using (
										Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None, chunkSize, true))
									{
										int bytesRead;
										int totalBytesRead = 0;
										do
										{
											bytesRead = await contentStream.ReadAsync(responseBuffer, 0, responseBuffer.Length, token);
											totalBytesRead += bytesRead;
											stream.Write(responseBuffer, 0, bytesRead);

											var progressArgs = new CourseDownloadingProgressArguments
											{
												CurrentAction = "Downloading",
												CourseName = course.Title,
												ClipName = fileName,
												CourseProgress = progressValue,
												ClipProgress = httpReponseMessage.Content.Headers.ContentLength != 0 ? (int) (((double) totalBytesRead)/ httpReponseMessage.Content.Headers.ContentLength*100): -1
											};

											downloadingProgress.Report(progressArgs);
										} while (bytesRead > 0);
									}
								}
							}

							timer.Enabled = true;
							File.Move($"{justFileName}.part", $"{justFileName}.mp4");
							await Task.Delay(timeout*1000, token);
							timer.Enabled = false;
							timeoutProgress.Report(0);
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

		public async Task<bool> GetProductsJsonAsync()
		{
			try
			{
			    var productsJsonResponse = await CreateRequest(HttpMethod.Get, new Uri("https://app.pluralsight.com/library/search/api?i=1&q1=course&x1=categories&count=6000"), null, AcceptHeader.HtmlXml, ContentType.AppXWwwFormUrlencode, "app.pluralsight.com");
				CachedProductsJson = productsJsonResponse.Content;
				//CachedProductsJson = await Task.Run<string>(() => { return File.ReadAllText("D:\\json.txt"); });
				return !string.IsNullOrEmpty(CachedProductsJson);
			}
			catch (Exception /*exc*/)
			{
				return false;
			}
		}

		public async Task<LoginResult> LoginAsync(string userName, string password)
		{
			var postData = BuildPostDataString(userName, password);
			var loginResult = new LoginResult() { DataJson = string.Empty, Status = LoginStatus.Failed };
			//loginResult.Status = LoginStatus.LoggedIn;
			//return loginResult;
			var loginResponse = await CreateRequest(HttpMethod.Post, new Uri("https://app.pluralsight.com/id/"), postData, AcceptHeader.HtmlXml, ContentType.AppXWwwFormUrlencode, "app.pluralsight.com");
			var loginContent = loginResponse.Content;
			if (!string.IsNullOrEmpty(loginContent))
			{
				var loginDoneRegExp = new Regex("{\"currentUser\":{.+\\:.+}");
				var loginDoneMatch = loginDoneRegExp.Match(loginContent);
				if (loginDoneMatch.Success)
				{
					loginResult.Status = LoginStatus.LoggedIn;
					_loggedIn = true;
					loginResult.DataJson = loginDoneMatch.Groups[0].Value;
					LoginResultJson = loginResult.DataJson;
					loginResult.Cookies = _cookies;
				}
				else
				{
					loginResult.Status = LoginStatus.Failed;
				}
			}
			else
			{
				loginResult.Status = LoginStatus.Failed;
			}
			return loginResult;
		}

		private string BuildPostDataString(string userName, string password)
		{
			return
				$"RedirectUrl=&Username={userName}&Password={password}&ShowCaptcha=False&ReCaptchaSiteKey=6LeVIgoTAAAAAIhx_TOwDWIXecbvzcWyjQDbXsaV";
		}

		private async Task<ResponseEx> CreateRequest(HttpMethod method, Uri url, string postData, AcceptHeader acceptHeader, ContentType contentType, string host)
		{
			try
			{
				var responseEx = new ResponseEx { Content = string.Empty, ContentLength = 0 };
				HttpRequestMessage requestMessage;

				var client = new HttpClient(new HttpClientHandler()
				{
					// preverent redirection after code 302
					AllowAutoRedirect = false,
					UseCookies = false
				});

				if(InitialiseHttpClient(ref client, method, url, postData, acceptHeader, contentType, host, out requestMessage))
				{
					var response = await client.SendAsync(requestMessage);
					var responseStream = await response.Content.ReadAsStreamAsync();

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
							var cookies = values.First().Split(';');
							_cookies += $"{cookies[0]}; ";
						}
						if (response.Headers.TryGetValues("Location", out values))
						{
							var redirectUrl = values.First();
							responseEx =
								await
									CreateRequest(HttpMethod.Get, new Uri(redirectUrl), null, AcceptHeader.HtmlXml,
										ContentType.AppXWwwFormUrlencode, "app.pluralsight.com");
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

				return new ResponseEx { Content = string.Empty, ContentLength = 0 };
			}
			catch (HttpRequestException /*exc*/)
			{
				return new ResponseEx { Content = string.Empty, ContentLength = 0 };
			}
		}

		string CreateFilePostJson(Clip clip, string clipQuality)
		{
			var stringParts = clip.PlayerUrl.Split('&');
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

		private bool InitialiseHttpClient(ref HttpClient httpClient, HttpMethod method, Uri url, string postData, AcceptHeader acceptHeader, ContentType contentType, string host, out HttpRequestMessage requestMessage)
		{
			try
			{
				requestMessage = new HttpRequestMessage {Method = method};

				// uncomment if you want to recieve gzipped response
				//requestMessage.Headers.AcceptEncoding.TryParseAdd("gzip,deflate");
				requestMessage.Headers.Accept.TryParseAdd(GetEnumDescription(acceptHeader));
				requestMessage.Headers.Host = host;

				requestMessage.Headers.Referrer = _loggedIn ? new Uri("https://www.pluralsight.com") : new Uri("https://app.pluralsight.com/id/");

				httpClient.BaseAddress = url;
				httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.5");
				httpClient.DefaultRequestHeaders.Add("User-agent", _userAgent);

				if (!string.IsNullOrEmpty(_cookies))
				{
					requestMessage.Headers.Add("Cookie", _cookies);
				}

				if (method == HttpMethod.Post)
				{
					requestMessage.Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(postData)));
					requestMessage.Content.Headers.Add("Content-Type", GetEnumDescription(contentType));
				}

				return true;
			}
			catch (Exception /*exc*/)
			{
				requestMessage = null;
				return false;
			}
		}
	}
}
