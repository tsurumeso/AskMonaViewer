namespace AskMonaViewer
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
    }
}
