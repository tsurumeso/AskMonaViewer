﻿using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;

namespace AskMonaWrapper
{
    public partial class AskMonaApi
    {
        internal async Task<T> CallAsync<T>(string url, Dictionary<string, string> prms)
        {
            var sb = new StringBuilder();
            foreach (var prm in prms)
            {
                if (prm.Value != null)
                    sb.Append(String.Format("{0}={1}&", prm.Key, System.Net.WebUtility.UrlEncode(prm.Value)));
            }

            try
            {
                var ub = new UriBuilder(url);
                ub.Query = sb.ToString().TrimEnd('&');
                var wc = new WebClientWithTimeout();
                var res = await wc.DownloadDataTaskAsync(ub.Uri);
                var serializer = new DataContractJsonSerializer(typeof(T));
                return (T)serializer.ReadObject(new MemoryStream(res));
            }
            catch { }

            return default(T);
        }

        internal async Task<T> CallAuthAsync<T>(string url, Dictionary<string, string> prms)
        {
            var authKey = await GenerateAuthorizationKey();
            if (authKey == null)
                return default(T);

            prms.Add("app_id", mApplicationId);
            prms.Add("u_id", mAccount.UserId.ToString());
            prms.Add("nonce", authKey.Nonce);
            prms.Add("time", authKey.Time);
            prms.Add("auth_key", authKey.Key);

            return await CallAsync<T>(url, prms);
        }

        internal static string GenerateNonce(int length)
        {
            var random = new Random();
            var nonceString = new StringBuilder();
            for (int i = 0; i < length; i++)
                nonceString.Append(mValidChars[random.Next(0, mValidChars.Length - 1)]);

            return nonceString.ToString();
        }

        internal async Task<AuthorizationKey> GenerateAuthorizationKey()
        {
            if (String.IsNullOrEmpty(mAccount.SecretKey) && 
                !String.IsNullOrEmpty(mAccount.Address) && !String.IsNullOrEmpty(mAccount.Password))
            {
                var secretKey = await FetchSecretKey(mAccount.Address, mAccount.Password);
                if (secretKey == null)
                    return null;

                mAccount.UserId = secretKey.UserId;
                mAccount.SecretKey = secretKey.Key;
            }

            var nonce = "";
            var time = "";
            var authKey = "+";
            while (authKey.Contains("+"))
            {
                nonce = GenerateNonce(32);
                time = DateTimeToUnixTimeStamp(DateTime.Now).ToString();
                var byteValues = Encoding.UTF8.GetBytes(mApplicationSecretKey + nonce + time + mAccount.SecretKey);
                authKey = Convert.ToBase64String(mSHA256Provider.ComputeHash(byteValues));
            }

            return new AuthorizationKey(authKey, nonce, time);
        }

        private static int DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }
    }
}
