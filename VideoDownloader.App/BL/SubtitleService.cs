using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using VideoDownloader.App.Contracts;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    class SubtitleService: ISubtitleService
    {
        public async Task<string> DownloadAsync(HttpHelper httpHelper, string authorId, int partNumber, string moduleName, CancellationToken token)
        {
            string postData = BuildSubtitlePostDataJson(authorId, partNumber, moduleName);
            ResponseEx response = await httpHelper.SendRequest(HttpMethod.Post,
                             new Uri(Properties.Settings.Default.SubtitlesUrl),
                             postData,
                             token);
            return response.Content;
        }

        public void Write(string fileName, IList<SrtRecord> subtitleRecords)
        {
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
