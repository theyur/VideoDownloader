using System;
using System.Linq;
using Newtonsoft.Json;

namespace VideoDownloader.App.Model
{
    public class CourseRpc
    {
        [JsonProperty(PropertyName = "id")]
        public string Name { get; set; }

        public string Title { get; set; }

        public Module[] Modules { get; set; }

        public Author[] Authors { get; set; }

        public bool CourseHasCaptions { get; set; }

        public bool SupportsWideScreenVideoFormats { get; set; }

        public string GetAuthorNameId(string authorId) =>
            Authors.Where(a => a.AuthorId == authorId).Select(a => a.Id).FirstOrDefault()
            ?? throw new ArgumentException($"Could not find author for id {authorId}");
    }
}
