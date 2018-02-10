using System.IO;
using VideoDownloader.App.Contracts;
using VideoDownloader.App.Properties;

namespace VideoDownloader.App.BL
{
    public class PluralsightMetadataService : ICourseMetadataService
    {
        public void WriteTableOfContent(string fileFullPath, string content)
        {
            Directory.CreateDirectory(fileFullPath);
            string tableOfCOntentFilePath = $"{fileFullPath}\\{Resources.TableOfContent}.txt";
            File.WriteAllText(tableOfCOntentFilePath, content);
        }

        public void WriteDescription(string fileFullPath, string content)
        {
            Directory.CreateDirectory(fileFullPath);
            string tableOfCOntentFilePath = $"{fileFullPath}\\{Resources.Description}.txt";
            File.WriteAllText(tableOfCOntentFilePath, content);
        }
    }
}
