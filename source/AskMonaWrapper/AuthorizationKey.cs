namespace AskMonaWrapper
{
    public class AuthorizationKey
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
