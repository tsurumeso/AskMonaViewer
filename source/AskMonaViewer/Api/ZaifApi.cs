using System;
using System.IO;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace AskMonaViewer.Api
{
    public class ZaifApi
    {
        private const string mApiBaseUrl = "https://api.zaif.jp/api/1/";
        private static HttpClient mHttpClient;

        public ZaifApi(HttpClient client)
        {
            mHttpClient = client;
        }

        private async Task<Stream> FetchResponseStreamAsync(string url)
        {
            try
            {
                var stream = await mHttpClient.GetStreamAsync(url);
                return stream;
            }
            catch { }
            return null;
        }

        public async Task<Currency> FetchRate(string currency)
        {
            var serializer = new DataContractJsonSerializer(typeof(Currency));
            var api = String.Format(mApiBaseUrl + "ticker/{0}", currency);
            
            var jsonStream = await FetchResponseStreamAsync(api);
            if (jsonStream == null)
                return null;

            var rate = (Currency)serializer.ReadObject(jsonStream);
            return rate;
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
