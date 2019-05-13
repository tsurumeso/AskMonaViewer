using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace AskMonaViewer.Utilities
{
    public class ZaifApi
    {
        private const string mApiBaseUrl = "https://api.zaif.jp/api/1/";

        private async Task<Stream> FetchResponseStreamAsync(string url)
        {
            try
            {
                var wc = new AskMonaWrapper.WebClientWithTimeout();
                var res = await wc.DownloadDataTaskAsync(url);
                return new MemoryStream(res);
            }
            catch { }
            return null;
        }

        public async Task<Currency> FetchPrice(string currency)
        {
            var serializer = new DataContractJsonSerializer(typeof(Currency));
            var api = String.Format(mApiBaseUrl + "ticker/{0}", currency);

            var jsonStream = await FetchResponseStreamAsync(api);
            if (jsonStream == null)
                return null;

            try
            {
                return (Currency)serializer.ReadObject(jsonStream);
            }
            catch
            {
                return null;
            }
        }
    }

    [DataContract]
    public class Currency
    {
        [DataMember(Name = "last")]
        public double Last { get; set; }

        [DataMember(Name = "high")]
        public double High { get; set; }

        [DataMember(Name = "low")]
        public double Low { get; set; }

        [DataMember(Name = "vwap")]
        public double Vwap { get; set; }

        [DataMember(Name = "volume")]
        public double Volume { get; set; }

        [DataMember(Name = "bid")]
        public double Bid { get; set; }

        [DataMember(Name = "ask")]
        public double Ask { get; set; }
    }
}
