using System;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.Contract
{
	public interface ICourseService
	{
		Task<LoginResult> LoginAsync(string userName, string password);

		Task<bool> GetProductsJsonAsync();

		string CachedProductsJson { get; set; }

		string LoginResultJson { get; set; }

		Task DownloadAsync(string productId, IProgress<CourseDownloadingProgressArguments> downloadingProgress, IProgress<int> timeoutProgress, CancellationToken token);
	}
}
