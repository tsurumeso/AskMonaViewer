using System.Collections.Generic;
using System.Runtime.Serialization;

namespace AskMonaWrapper
{ 
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
    public class NGUsers : ApiResult
    {
        [DataMember(Name = "users")]
        public List<User> Users { get; set; }

        public NGUsers()
        {
            Users = new List<User>();
        }
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

        [DataMember(Name = "u_title")]
        public string UserTitle { get; set; }

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

        [DataMember(Name = "receive")]
        public string Receive { get; set; }

        [DataMember(Name = "favorites")]
        public int Favorites { get; set; }

        [DataMember(Name = "editable")]
        public int Editable { get; set; }

        [DataMember(Name = "sh_host")]
        public int ShowHost { get; set; }

        [DataMember]
        public int Scrolled { get; set; }

        [DataMember]
        public int Increased { get; set; }

        [DataMember]
        public int CachedCount { get; set; }
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

        [DataMember(Name = "u_title")]
        public string UserTitle { get; set; }

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
