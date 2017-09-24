using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoDownloader.App.Contract;
using VideoDownloader.App.Model;

namespace VideoDownloader.App.BL
{
    class SubtitleService: ISubtitleService
    {
        public void Write(string fileName, IList<SrtRecord> subtitleRecords)
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                int index = 1;
                foreach (var record in subtitleRecords)
                {
                    sw.WriteLine(index);
                    sw.WriteLine($"{record.FromTimeSpan:hh\\:mm\\:ss\\.FFF} --> {record.ToTimeSpan:hh\\:mm\\:ss\\.FFF}");
                    sw.WriteLine(record.Text);
                    sw.WriteLine();
                    ++index;
                }
            }
        }
    }
}
