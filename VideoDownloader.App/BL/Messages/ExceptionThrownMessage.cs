using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;

namespace VideoDownloader.App.BL.Messages
{
    class ExceptionThrownMessage: MessageBase
    {
        public ExceptionThrownMessage(string messageText)
        {
            Text = messageText;
        }

        public string Text { get; set; }
    }
}
