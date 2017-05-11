using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.Contract
{
	public interface ICourseService
	{

		Task<bool> DownloadProductsJsonAsync();

		string CachedProductsJson { get; set; }

	    string Cookies { get; set; }

	    Dictionary<string, Tool> Tools { get; set; }
	    Task DownloadAsync(string productId, IProgress<CourseDownloadingProgressArguments> downloadingProgress, IProgress<int> timeoutProgress, CancellationToken token);
	}
}
