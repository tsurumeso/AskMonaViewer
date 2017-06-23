using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;

namespace AskMonaViewer
{
    public class AskMonaApi
    {
        private const string mApiBaseUrl = "http://askmona.org/v1/";
        private static HttpClient mHttpClient;
        private static SHA256CryptoServiceProvider mSHA256Provider;
        private const int mApplicationId = 3738;
        private const string mApplicationSecretKey = "AgGu661B9pe9SL49soov7tZNYRzdF4n8TUjsqNUTOTu0=";
        private const string mValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private DateTime mUnixEpoch;
        private Account mAccount;

        public AskMonaApi(Account account)
        {
            mHttpClient = new HttpClient();
            mHttpClient.Timeout = TimeSpan.FromSeconds(10.0);
            mSHA256Provider = new SHA256CryptoServiceProvider();
            mUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            mAccount = account;
        }

        private async Task<Stream> FetchHtmlStreamAsync(string url)
        {
            try
            {
                var stream = await mHttpClient.GetStreamAsync(url);
                return stream;
            }
            catch { }
            return null;
        }

        private static string GenerateNonce(int length)
        {
            var random = new Random();
            var nonceString = new StringBuilder();
            for (int i = 0; i < length; i++)
                nonceString.Append(mValidChars[random.Next(0, mValidChars.Length - 1)]);

            return nonceString.ToString();
        }

        private async Task<string> GenerateAuthorizationKey(string nonce, string time)
        {
            if (String.IsNullOrEmpty(mAccount.SecretKey))
            {
                var secretKey = await FetchSecretKey();
                if (secretKey == null)
                    return null;

                mAccount.UserId = secretKey.UserId;
                mAccount.SecretKey = secretKey.Key;
            }

            var byteValues = Encoding.UTF8.GetBytes(mApplicationSecretKey + nonce + time + mAccount.SecretKey);
            var hash256Value = mSHA256Provider.ComputeHash(byteValues);

            return Convert.ToBase64String(hash256Value);
        }

        private async Task<SecretKey> FetchSecretKey()
        {
            if (String.IsNullOrEmpty(mAccount.Address) || String.IsNullOrEmpty(mAccount.Password))
                return null;

            var serializer = new DataContractJsonSerializer(typeof(SecretKey));
            var api = String.Format(mApiBaseUrl + 
                "auth/secretkey?app_id={0}&app_secretkey={1}&u_address={2}&pass={3}",
                mApplicationId, mApplicationSecretKey, mAccount.Address, mAccount.Password);
            var jsonStream = await FetchHtmlStreamAsync(api);

            if (jsonStream == null)
                return null;

            return (SecretKey)serializer.ReadObject(jsonStream);
        }

        public async Task<PostResult> PostResponseAsync(int t_id, string text, int sage)
        {
            var nonce = "";
            var time = "";
            var authKey = "+";
            while (authKey.Contains("+"))
            {
                nonce = GenerateNonce(32);
                time = ((long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds).ToString();
                authKey = await GenerateAuthorizationKey(nonce, time);
                if (authKey == null)
                    return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(PostResult));
            var api = String.Format(mApiBaseUrl + 
                "responses/post?app_id={0}&u_id={1}&nonce={2}&time={3}&auth_key={4}&t_id={5}&text={6}&sage={7}",
                mApplicationId, mAccount.UserId, nonce, time, authKey, t_id, text, sage);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (PostResult)serializer.ReadObject(jsonStream);
        }

        public async Task<Balance> FetchBlanceAsync(int detail = 0)
        {
            var nonce = "";
            var time = "";
            var authKey = "+";
            while (authKey.Contains("+"))
            {
                nonce = GenerateNonce(32);
                time = ((long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds).ToString();
                authKey = await GenerateAuthorizationKey(nonce, time);
                if (authKey == null)
                    return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(Balance));
            var api = String.Format(mApiBaseUrl + 
                "account/balance?app_id={0}&u_id={1}&nonce={2}&time={3}&auth_key={4}&detail={5}",
                mApplicationId, mAccount.UserId, nonce, time, authKey, detail);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (Balance)serializer.ReadObject(jsonStream);
        }

        public async Task<SendResult> SendMonaAsync(int to_u_id, ulong amount, int anonymous = 1, string msg_text = "", int sage = 0)
        {
            var nonce = "";
            var time = "";
            var authKey = "+";
            while (authKey.Contains("+"))
            {
                nonce = GenerateNonce(32);
                time = ((long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds).ToString();
                authKey = await GenerateAuthorizationKey(nonce, time);
                if (authKey == null)
                    return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(SendResult));
            var api = String.Format(mApiBaseUrl +
                "account/send?app_id={0}&u_id={1}&nonce={2}&time={3}&auth_key={4}&to_u_id={5}&amount={6}&anonymous={7}&msg_text={8}&sage={9}",
                mApplicationId, mAccount.UserId, nonce, time, authKey, to_u_id, amount, anonymous, msg_text, sage);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (SendResult)serializer.ReadObject(jsonStream);
        }

        public async Task<SendResult> SendMonaAsync(int t_id, int r_id, ulong amount, int anonymous = 1, string msg_text = "", int sage = 0)
        {
            var nonce = "";
            var time = "";
            var authKey = "+";
            while (authKey.Contains("+"))
            {
                nonce = GenerateNonce(32);
                time = ((long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds).ToString();
                authKey = await GenerateAuthorizationKey(nonce, time);
                if (authKey == null)
                    return null;
            }

            var serializer = new DataContractJsonSerializer(typeof(SendResult));
            var api = String.Format(mApiBaseUrl +
                "account/send?app_id={0}&u_id={1}&nonce={2}&time={3}&auth_key={4}&t_id={5}&r_id={6}&amount={7}&anonymous={8}&msg_text={9}&sage={10}",
                mApplicationId, mAccount.UserId, nonce, time, authKey, t_id, r_id, amount, anonymous, msg_text, sage);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (SendResult)serializer.ReadObject(jsonStream);
        }

        public async Task<TopicList> FetchTopicListAsync(int cat_id = 0, int limit = 100)
        {
            var serializer = new DataContractJsonSerializer(typeof(TopicList));
            var api = String.Format(mApiBaseUrl + "topics/list?cat_id={0}&limit={1}", cat_id, limit);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (TopicList)serializer.ReadObject(jsonStream);
        }

        public async Task<ResponseList> FetchResponseListAsync(int t_id, int from = 1, int to = 1000)
        {
            var serializer = new DataContractJsonSerializer(typeof(ResponseList));
            var api = String.Format(mApiBaseUrl + "responses/list?t_id={0}&from={1}&to={2}", t_id, from, to);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            return (ResponseList)serializer.ReadObject(jsonStream);
        }

        public async Task<Response> FetchResponseAsync(int t_id, int r_id = 1)
        {
            var serializer = new DataContractJsonSerializer(typeof(ResponseList));
            var api = String.Format(mApiBaseUrl + "responses/list?t_id={0}&from={1}", t_id, r_id);

            var jsonStream = await FetchHtmlStreamAsync(api);
            if (jsonStream == null)
                return null;

            var responseList = (ResponseList)serializer.ReadObject(jsonStream);
            return responseList.Responses[0];
        }
    }

    [DataContract]
    public class PostResult
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    public class SendResult
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "balance")]
        public string Balance { get; set; }
    }

    [DataContract]
    public class Balance
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "balance")]
        public string Value { get; set; }

        [DataMember(Name = "accounts")]
        public BalanceDetails Accounts { get; set; }
    }

    [DataContract]
    public class BalanceDetails
    {
        [DataMember(Name = "deposit")]
        public string Deposit { get; set; }

        [DataMember(Name = "send")]
        public string Send { get; set; }

        [DataMember(Name = "receivee")]
        public string Receive { get; set; }

        [DataMember(Name = "withdraw")]
        public string Withdraw { get; set; }

        [DataMember(Name = "gift")]
        public string Gift { get; set; }

        [DataMember(Name = "reserved")]
        public string Reserved { get; set; }

        [DataMember(Name = "balance")]
        public string Balance { get; set; }
    }

    [DataContract]
    public class SecretKey
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "u_id")]
        public int UserId { get; set; }

        [DataMember(Name = "secretkey")]
        public string Key { get; set; }
    }

    [DataContract]
    public class TopicList
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }

        [DataMember(Name = "topics")]
        public List<Topic> Topics { get; set; }

        public TopicList()
        {
            Topics = new List<Topic>();
        }
    }

    [DataContract]
    public class Topic
    {
        [DataMember(Name = "rank")]
        public int Rank { get; set; }

        [DataMember(Name = "t_id")]
        public int Id { get; set; }

        [DataMember(Name = "u_id")]
        public int UserId { get; set; }

        [DataMember(Name = "state")]
        public int State { get; set; }

        [DataMember(Name = "title")]
        public string Title { get; set; }

        [DataMember(Name = "cat_id")]
        public int CategoryId { get; set; }

        [DataMember(Name = "category")]
        public string Category { get; set; }

        [DataMember(Name = "tags")]
        public string Tags { get; set; }

        [DataMember(Name = "lead")]
        public string Lead { get; set; }

        [DataMember(Name = "ps")]
        public string Supplyment { get; set; }

        [DataMember(Name = "created")]
        public int Created { get; set; }

        [DataMember(Name = "updated")]
        public int Updated { get; set; }

        [DataMember(Name = "modified")]
        public int Modified { get; set; }

        [DataMember(Name = "count")]
        public int Count { get; set; }

        public int Increased { get; set; }

        [DataMember(Name = "receive")]
        public string ReceivedMona { get; set; }

        [DataMember(Name = "favorites")]
        public int Favorites { get; set; }

        [DataMember(Name = "editable")]
        public int Editable { get; set; }

        [DataMember(Name = "sh_host")]
        public int IsHost { get; set; }
    }

    [DataContract]
    public class ResponseList
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }

        [DataMember(Name = "updated")]
        public int Updated { get; set; }

        [DataMember(Name = "modified")]
        public int Modified { get; set; }

        [DataMember(Name = "topic")]
        public Topic Topic { get; set; }

        [DataMember(Name = "responses")]
        public List<Response> Responses { get; set; }

        public ResponseList()
        {
            Responses = new List<Response>();
        }
    }

    [DataContract]
    public class Response
    {
        [DataMember(Name = "r_id")]
        public int Id { get; set; }

        [DataMember(Name = "state")]
        public int State { get; set; }

        [DataMember(Name = "created")]
        public int Created { get; set; }

        [DataMember(Name = "u_id")]
        public int UserId { get; set; }

        [DataMember(Name = "u_name")]
        public string UserName { get; set; }

        [DataMember(Name = "u_dan")]
        public string UserDan { get; set; }

        [DataMember(Name = "u_times")]
        public string UserTimes { get; set; }

        [DataMember(Name = "receive")]
        public string ReceivedMona { get; set; }

        [DataMember(Name = "res_lv")]
        public int ReceivedLevel { get; set; }

        [DataMember(Name = "rec_count")]
        public int ReceivedCount { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "response")]
        public string Text { get; set; }
    }
}
