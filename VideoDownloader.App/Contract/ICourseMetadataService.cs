using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.App.Contract
{
    public interface ICourseMetadataService
    {
        void WriteTableOfContent(string fileFullPath, string content);
        void WriteDescription(string fileFullPath, string content);
    }
}
