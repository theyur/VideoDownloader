using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Contracts;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    class SubtitleService: ISubtitleService
    {
        public async Task<string> DownloadAsync(HttpHelper httpHelper, string authorId, int partNumber, string moduleName, string clipId, CancellationToken token)
        {
            /*
            string postData = BuildSubtitlePostDataJson(authorId, partNumber, moduleName);
            ResponseEx response = await httpHelper.SendRequest(HttpMethod.Post,
                             new Uri(Properties.Settings.Default.SubtitlesUrl),
                             postData,
                             Properties.Settings.Default.RetryOnRequestFailureCount, token);
            */

            var response = await httpHelper.SendRequest(
                HttpMethod.Get,
                new Uri($"https://app.pluralsight.com/transcript/api/v1/caption/json/{clipId}/en"),
                String.Empty,
                Properties.Settings.Default.RetryOnRequestFailureCount,
                token);

            return response.Content;
        }

        public void Write(string fileName, IList<SrtRecord> subtitleRecords)
        {
            if(!subtitleRecords.Any()) return;
            
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                int index = 1;
                foreach (var record in subtitleRecords)
                {
                    sw.WriteLine(index);
                    sw.WriteLine($"{record.FromTimeSpan:hh':'mm':'ss'.'fff} --> {record.ToTimeSpan:hh':'mm':'ss'.'fff}");
                    sw.WriteLine(record.Text);
                    sw.WriteLine();
                    ++index;
                }
            }
        }

        private string BuildSubtitlePostDataJson(string authorId, int partNumber, string moduleName)
        {
            SubtitlePostData viewclipData = new SubtitlePostData()
            {
                Author = authorId,
                ClipIndex = partNumber,
                Locale = Properties.Settings.Default.EnglishLocale,
                ModuleName = moduleName
            };
            return Newtonsoft.Json.JsonConvert.SerializeObject(viewclipData);
        }
    }
}
