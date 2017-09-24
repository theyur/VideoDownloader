using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.App.Model
{
    public class SrtRecord
    {
        public TimeSpan FromTimeSpan { get; set; }

        public TimeSpan ToTimeSpan { get; set; }

        public string Text { get; set; }
    }
}
