namespace VideoDownloader.App.Contracts
{
    public interface ICourseMetadataService
    {
        void WriteTableOfContent(string fileFullPath, string content);
        void WriteDescription(string fileFullPath, string content);
    }
}
