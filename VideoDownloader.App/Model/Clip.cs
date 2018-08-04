namespace VideoDownloader.App.Model
{
    public class Clip
    {
        public bool Authorized { get; set; }

        public int Index { get; set; }

        public int ModuleIndex { get; set; }

        public string Title { get; set; }

        public string Duration { get; set; }

        public string FormattedDuration { get; set; }

        public string Name { get; set; }

        public bool Watched { get; set; }

        public string Id { get; set; }
        public string ModuleTitle { get; set; }
    }
}
