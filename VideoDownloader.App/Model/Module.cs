namespace VideoDownloader.App.Model
{
    public class Module
    {
        public string Name { get; set; }

        public string Title { get; set; }

        public string Duration { get; set; }

        public string FormattedDuration { get; set; }

        public string Author { get; set; }

        public Clip[] Clips { get; set; }

        public string ModuleId { get; set; }

        public string AuthorId { get; set; }
    }
}
