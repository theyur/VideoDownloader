using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Model;
using VideoDownloader.App.ViewModel;

namespace VideoDownloader.App.Contracts
{
    public interface ICourseService
    {

        Task<bool> ProcessNoncachedProductsJsonAsync();

        Task<bool> ProcessCachedProductsAsync();

        string CachedProductsJson { get; }

        string Cookies { get; set; }

        Dictionary<string, List<CourseDescription>> CoursesByToolName { get; set; }

        Task DownloadAsync(string productId, IProgress<CourseDownloadingProgressArguments> downloadingProgress, IProgress<int> timeoutProgress,
            CancellationToken token, PluralsightMainViewModel.LastFinishedMessageComposer lastFinishedMessage);

        Task<string> GetTableOfContentAsync(string productId, CancellationToken token);

        Task<string> GetFullDescriptionAsync(string productId, CancellationToken token);

        Task<List<CourseDescription>> GetToolCourses(string toolName);

        string GetBaseCourseDirectoryName(string destinationDirectory, string courseName);
    }
}
