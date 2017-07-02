using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace AskMonaViewer
{
    public partial class AskMonaApi
    {
        internal async Task<T> CallAsync<T>(string url, Dictionary<string, string> prms)
        {
            var ub = new UriBuilder(url);
            var sb = new StringBuilder();
            bool isInitial = true;
            foreach (var prm in prms)
            {
                if (!isInitial)
                    sb.Append("&");
                else
                    isInitial = false;
                sb.Append(String.Format("{0}={1}", prm.Key, WebUtility.UrlEncode(prm.Value)));
            }
            ub.Query = sb.ToString();
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(T));
                var stream = await mHttpClient.GetStreamAsync(ub.Uri);
                return (T)serializer.ReadObject(stream);
            }
            catch { }
            return default(T);
        }

        internal async Task<T> CallAuthAsync<T>(string url, Dictionary<string, string> prms)
        {
            var authKey = await GenerateAuthorizationKey();
            if (authKey == null)
                return default(T);

            prms.Add("app_id", mApplicationId.ToString());
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
                time = ((long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds).ToString();
                var byteValues = Encoding.UTF8.GetBytes(mApplicationSecretKey + nonce + time + mAccount.SecretKey);
                authKey = Convert.ToBase64String(mSHA256Provider.ComputeHash(byteValues));
            }

            return new AuthorizationKey(authKey, nonce, time);
        }
    }

    internal class AuthorizationKey
    {
        public string Key;
        public string Nonce;
        public string Time;

        public AuthorizationKey(string key, string nonce, string time)
        {
            Key = key;
            Nonce = nonce;
            Time = time;
        }
    }
}
