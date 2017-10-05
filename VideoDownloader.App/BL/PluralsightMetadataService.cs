using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
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
