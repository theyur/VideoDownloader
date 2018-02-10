namespace VideoDownloader.App.Contracts
{
    public interface IConfigProvider
    {
        int MinTimeout { get; set; }

        string UserAgent { get; }

        int MaxTimeout{get;set;}

        string DownloadsPath {get;set;}

        void Save();
    }
}
