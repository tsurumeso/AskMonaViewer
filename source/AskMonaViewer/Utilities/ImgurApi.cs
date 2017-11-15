using System;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Collections.Specialized;
using System.Runtime.Serialization.Json;

namespace AskMonaViewer.Utilities
{
    class ImgurApi
    {
        private const string mApiBaseUrl = "https://api.imgur.com/3/";
        private string mClientId = "";

        public ImgurApi(string clientId)
        {
            mClientId = clientId;
        }

        public async Task<UploadStatus> UploadImage(Image image)
        {
            string base64Image = "";
            using (var m = new MemoryStream())
            {
                image.Save(m, image.RawFormat);
                byte[] imageBytes = m.ToArray();
                base64Image = Convert.ToBase64String(imageBytes);
            }

            var values = new NameValueCollection
            {
                { "image", base64Image },
                { "type", "base64"}
            };

            byte[] res = null;
            try
            {
                await Task.Run(() =>
                {
                    var client = new WebClientWithTimeout();
                    client.Headers.Add("Authorization", "Client-ID " + mClientId);
                    res = client.UploadValues(new Uri(mApiBaseUrl + "image"), values);
                });
                var serializer = new DataContractJsonSerializer(typeof(UploadStatus));
                return (UploadStatus)serializer.ReadObject(new MemoryStream(res));
            }
            catch
            {
                return null;
            }
        }
    }

    [DataContract]
    public class UploadStatus
    {
        [DataMember(Name = "data")]
        public ImgurImage Data { get; set; }

        [DataMember(Name = "success")]
        public bool Success { get; set; }

        [DataMember(Name = "status")]
        public int Status { get; set; }
    }

    [DataContract]
    public class ImgurImage
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }
    }
}
