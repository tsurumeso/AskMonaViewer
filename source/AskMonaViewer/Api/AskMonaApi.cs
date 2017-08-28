﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AskMonaViewer.Api
{
    public partial class AskMonaApi
    {
        private const string mApiBaseUrl = "http://askmona.org/v1/";
        private static HttpClient mHttpClient;
        private static SHA256CryptoServiceProvider mSHA256Provider;
        private const string mApplicationId = "3738";
        private const string mApplicationSecretKey = "AgGu661B9pe9SL49soov7tZNYRzdF4n8TUjsqNUTOTu0=";
        private const string mValidChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private Account mAccount;

        public int UserId
        {
            get
            {
                return mAccount.UserId;
            }
        }

        public AskMonaApi(HttpClient client, Account account)
        {
            mHttpClient = client;
            mSHA256Provider = new SHA256CryptoServiceProvider();
            mAccount = account;
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
    }

    [DataContract]
    public class ApiResult
    {
        [DataMember(Name = "status")]
        public int Status { get; set; }

        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    [DataContract]
    public class TransactionDetail : ApiResult
    {
        [DataMember(Name = "transactions")]
        public List<Transaction> Transactions { get; set; }

        public TransactionDetail()
        {
            Transactions = new List<Transaction>();
        }
    }

    [DataContract]
    public class Transaction
    {
        [DataMember(Name = "created")]
        public int Created { get; set; }

        [DataMember(Name = "item")]
        public string Item { get; set; }

        [DataMember(Name = "amount")]
        public string Amount { get; set; }

        [DataMember(Name = "t_id")]
        public int TopicId { get; set; }

        [DataMember(Name = "r_id")]
        public int ResponceId { get; set; }

        [DataMember(Name = "anonymous")]
        public int Anonymous { get; set; }

        [DataMember(Name = "user")]
        public User User { get; set; }

        [DataMember(Name = "msg_text")]
        public string Message { get; set; }
    }

    [DataContract]
    public class User
    {
        [DataMember(Name = "u_id")]
        public int UserId { get; set; }

        [DataMember(Name = "u_name")]
        public string UserName { get; set; }

        [DataMember(Name = "u_dan")]
        public string UserDan { get; set; }
    }

    [DataContract]
    public class SecretKey : ApiResult
    {
        [DataMember(Name = "u_id")]
        public int UserId { get; set; }

        [DataMember(Name = "secretkey")]
        public string Key { get; set; }
    }

    [DataContract]
    public class Balance : ApiResult
    {
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

        [DataMember(Name = "receive")]
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
    public class DepositAddress : ApiResult
    {
        [DataMember(Name = "d_address")]
        public string Address { get; set; }
    }

    [DataContract]
    public class TopicList : ApiResult
    {
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
    public class UserProfile : ApiResult
    {
        [DataMember(Name = "u_name")]
        public string UserName { get; set; }

        [DataMember(Name = "u_dan")]
        public string UserDan { get; set; }

        [DataMember(Name = "profile")]
        public string Text { get; set; }
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

        public System.Drawing.Point Scrolled { get; set; }

        public int CachedCount { get; set; }

        [DataMember(Name = "receive")]
        public string Receive { get; set; }

        [DataMember(Name = "favorites")]
        public int Favorites { get; set; }

        [DataMember(Name = "editable")]
        public int Editable { get; set; }

        [DataMember(Name = "sh_host")]
        public int IsHost { get; set; }

        public Topic()
        {
            Scrolled = new System.Drawing.Point(0, 0);
        }
    }

    [DataContract]
    public class ResponseList : ApiResult
    {
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
        public string Receive { get; set; }

        [DataMember(Name = "res_lv")]
        public int Level { get; set; }

        [DataMember(Name = "rec_count")]
        public int ReceivedCount { get; set; }

        [DataMember(Name = "host")]
        public string Host { get; set; }

        [DataMember(Name = "response")]
        public string Text { get; set; }
    }
}