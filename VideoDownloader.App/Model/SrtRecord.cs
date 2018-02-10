using System;

namespace VideoDownloader.App.Model
{
    public class SrtRecord
    {
        public TimeSpan FromTimeSpan { get; set; }

        public TimeSpan ToTimeSpan { get; set; }

        public string Text { get; set; }
    }
}
