using System;
using System.IO;
using System.Drawing;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Collections.Specialized;
using System.Runtime.Serialization.Json;

namespace AskMonaViewer.Utilities
{
    public class ImgurApi
    {
        private const string mApiBaseUrl = "https://api.imgur.com/3/";
        private string mClientId = "";

        public ImgurApi(string clientId)
        {
            mClientId = clientId;
        }

        public async Task<ImgurImage> UploadImage(Image image)
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

            try
            {
                byte[] res = null;
                await Task.Run(() =>
                {
                    var client = new AskMonaWrapper.WebClientWithTimeout();
                    client.Headers.Add("Authorization", "Client-ID " + mClientId);
                    res = client.UploadValues(mApiBaseUrl + "image", "POST", values);
                });
                var serializer = new DataContractJsonSerializer(typeof(ImgurRootObject<ImgurImage>));
                var imgurRootObject = (ImgurRootObject<ImgurImage>)serializer.ReadObject(new MemoryStream(res));
                return imgurRootObject.Data;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> DeleteImage(string deleteHash)
        {
            try
            {
                byte[] res = null;
                await Task.Run(() =>
                {
                    var client = new AskMonaWrapper.WebClientWithTimeout();
                    client.Headers.Add("Authorization", "Client-ID " + mClientId);
                    res = client.UploadValues(mApiBaseUrl + "image/" + deleteHash, "DELETE", new NameValueCollection());
                });
                var serializer = new DataContractJsonSerializer(typeof(ImgurRootObject<bool>));
                var imgurRootObject = (ImgurRootObject<bool>)serializer.ReadObject(new MemoryStream(res));
                return imgurRootObject.Data;
            }
            catch
            {
                return false;
            }
        }
    }

    [DataContract]
    public class ImgurRootObject<T>
    {
        [DataMember(Name = "data")]
        public T Data { get; set; }

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

        [DataMember(Name = "datetime")]
        public int DateTime { get; set; }

        [DataMember(Name = "width")]
        public int Width { get; set; }

        [DataMember(Name = "height")]
        public int Height { get; set; }

        [DataMember(Name = "size")]
        public int Size { get; set; }

        [DataMember(Name = "deletehash")]
        public string DeleteHash { get; set; }

        [DataMember(Name = "link")]
        public string Link { get; set; }
    }
}
