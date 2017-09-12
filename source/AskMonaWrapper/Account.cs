using System.Text.RegularExpressions;

namespace AskMonaWrapper
{
    public class Account
    {
        public string Address { get; set; }
        public string Password { get; set; }
        public int UserId { get; set; }
        public string SecretKey { get; set; }

        public Account()
        {
            Address = "";
            Password = "";
            UserId = -1;
            SecretKey = "";
        }

        public Account(string addr, string pass)
        {
            Address = addr;
            Password = pass;
            UserId = -1;
            SecretKey = "";
        }

        public Account FromAuthCode(string code)
        {
            code = code.Replace("\\", "");
            var m = Regex.Match(code, @"""u_id"":(?<UserId>[0-9]+),""secretkey"":""(?<SecretKey>.+)""");
            if (m.Success)
            {
                UserId = int.Parse(m.Groups["UserId"].Value);
                SecretKey = m.Groups["SecretKey"].Value;
            }
            return this;
        }
    }
}
