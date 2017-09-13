using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AskMonaWrapper
{
    public partial class AskMonaApi
    {
        private const string mApiBaseUrl = "http://askmona.org/v1/";
        private static HttpClient mHttpClient;
        private static SHA256CryptoServiceProvider mSHA256Provider;
        private string mApplicationId;
        private string mApplicationSecretKey;
        private const string mValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private Account mAccount;

        public int UserId
        {
            get
            {
                return mAccount.UserId;
            }
        }

        public Account Account
        {
            set
            {
                mAccount = value;
            }
        }

        public AskMonaApi(HttpClient client, string appId, string appSecretKey, Account account)
        {
            mHttpClient = client;
            mSHA256Provider = new SHA256CryptoServiceProvider();
            mAccount = account;
            mApplicationId = appId;
            mApplicationSecretKey = appSecretKey;
        }

        internal async Task<SecretKey> FetchSecretKey(string addr, string pass)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("app_id", mApplicationId);
            prms.Add("app_secretkey", mApplicationSecretKey);
            prms.Add("u_address", addr);
            prms.Add("pass", pass);

            return await CallAsync<SecretKey>(mApiBaseUrl + "auth/secretkey", prms);
        }

        public async Task<ApiResult> PostResponseAsync(int t_id, string text, int sage)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());
            prms.Add("text", text);
            prms.Add("sage", sage.ToString());

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "responses/post", prms);
        }

        public async Task<Balance> FetchBlanceAsync(int detail = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("detail", detail.ToString());

            return await CallAuthAsync<Balance>(mApiBaseUrl + "account/balance", prms);
        }

        public async Task<Balance> SendMonaAsync(int to_u_id, ulong amount, int anonymous = 1, string msg_text = "", int sage = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("to_u_id", to_u_id.ToString());
            prms.Add("amount", amount.ToString());
            prms.Add("anonymous", anonymous.ToString());
            prms.Add("msg_text", msg_text);
            prms.Add("sage", sage.ToString());

            return await CallAuthAsync<Balance>(mApiBaseUrl + "account/send", prms);
        }

        public async Task<Balance> SendMonaAsync(int t_id, int r_id, ulong amount, int anonymous = 1, string msg_text = "", int sage = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());
            prms.Add("r_id", r_id.ToString());
            prms.Add("amount", amount.ToString());
            prms.Add("anonymous", anonymous.ToString());
            prms.Add("msg_text", msg_text);
            prms.Add("sage", sage.ToString());

            return await CallAuthAsync<Balance>(mApiBaseUrl + "account/send", prms);
        }

        public async Task<Balance> WithdrawMonaAsync(ulong amount)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("amount", amount.ToString());

            return await CallAuthAsync<Balance>(mApiBaseUrl + "account/withdraw", prms);
        }

        public async Task<DepositAddress> FetchDepositAddressAsync()
        {
            var prms = new Dictionary<string, string>();

            return await CallAuthAsync<DepositAddress>(mApiBaseUrl + "account/deposit", prms);
        }

        public async Task<ApiResult> CreateTopicAsync(string title, string text, int cat_id, string tags)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("title", title);
            prms.Add("text", text);
            prms.Add("cat_id", cat_id.ToString());
            prms.Add("tags", tags);

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "topics/new", prms);
        }

        public async Task<ApiResult> EditTopicAsync(int t_id, int cat_id, string tags, string lead, string ps)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());
            prms.Add("cat_id", cat_id.ToString());
            prms.Add("tags", tags);
            prms.Add("lead", lead);
            prms.Add("ps", ps);

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "topics/edit", prms);
        }

        public async Task<TopicList> FetchTopicListAsync(int cat_id = 0, int limit = 100, int offset = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("cat_id", cat_id.ToString());
            prms.Add("limit", limit.ToString());
            prms.Add("offset", offset.ToString());

            return await CallAsync<TopicList>(mApiBaseUrl + "topics/list", prms);
        }

        public async Task<TopicList> FetchFavoriteTopicListAsync(int limit = 200, int offset = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("limit", limit.ToString());
            prms.Add("offset", offset.ToString());

            return await CallAuthAsync<TopicList>(mApiBaseUrl + "favorites/list", prms);
        }

        public async Task<ApiResult> AddFavoriteTopicAsync(int t_id)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "favorites/add", prms);
        }

        public async Task<ApiResult> DeleteFavoriteTopicAsync(int t_id)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "favorites/delete", prms);
        }

        public async Task<ResponseList> FetchResponseListAsync(int t_id, int from = 1, int to = 1000, int topic_detail = 0, long prev = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("t_id", t_id.ToString());
            prms.Add("from", from.ToString());
            prms.Add("to", to.ToString());
            prms.Add("topic_detail", topic_detail.ToString());
            prms.Add("if_modified_since", prev.ToString());

            return await CallAsync<ResponseList>(mApiBaseUrl + "responses/list", prms);
        }

        public async Task<UserProfile> FetchUserProfileAsync(int u_id)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("u_id", u_id.ToString());

            return await CallAsync<UserProfile>(mApiBaseUrl + "users/profile", prms);
        }

        public async Task<UserProfile> EditUserProfileAsync(string u_name=null, string profile=null)
        {
            var prms = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(u_name))
                prms.Add("u_name", u_name);
            if (!String.IsNullOrEmpty(profile))
                prms.Add("profile", profile);

            return await CallAuthAsync<UserProfile>(mApiBaseUrl + "users/myprofile", prms);
        }

        public async Task<TransactionDetail> FetchTransactionAsync(string item, int limit = 200, int offset = 0)
        {
            var prms = new Dictionary<string, string>();
            prms.Add("item", item);
            prms.Add("limit", limit.ToString());
            prms.Add("offset", offset.ToString());

            return await CallAuthAsync<TransactionDetail>(mApiBaseUrl + "account/txdetail", prms);
        }

        public async Task<ApiResult> VerifySecretKey()
        {
            var prms = new Dictionary<string, string>();

            return await CallAuthAsync<ApiResult>(mApiBaseUrl + "auth/verify", prms);
        }
    }
}
