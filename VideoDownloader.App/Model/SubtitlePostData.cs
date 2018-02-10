using Newtonsoft.Json;

namespace VideoDownloader.App.Model
{
    class SubtitlePostData
    {
        [JsonProperty("a")]
        public string Author { get; set; }

        [JsonProperty("cn")]
        public int ClipIndex { get; set; }

        [JsonProperty("lc")]
        public string Locale { get; set; }
       
        [JsonProperty("m")]
        public string ModuleName { get; set; }
    }
}
