using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.Contract
{
    interface ISubtitleService
    {
        void Write(string fileName, IList<SrtRecord> subtitleRecords);

    }
}
