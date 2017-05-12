using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoDownloader.App.Model
{
    public class PayloadRpc
    {
        public dynamic Profile { get; set; }

        public CourseRpc Course { get; set; }
    }
}
