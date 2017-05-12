using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VideoDownloader.App.Model
{
    class RpcData
    {
        public bool Success { get; set; }

        public PayloadRpc Payload { get; set; }

        public dynamic Trace { get; set; }
    }
}
