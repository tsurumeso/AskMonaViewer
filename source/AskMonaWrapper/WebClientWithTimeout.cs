using System;
using System.Net;

namespace AskMonaWrapper
{
    public class WebClientWithTimeout : WebClient
    {
        public int Timeout { get; set; } = 10 * 1000;

        protected override WebRequest GetWebRequest(Uri address)
        {
            var w = base.GetWebRequest(address);
            w.Timeout = Timeout;
            return w;
        }
    }
}
