namespace AskMonaViewer.Settings
{
    public class ApplicationSettings
    {
        public Account Account { get; set; }
        public MainFormSettings MainFormSettings { get; set; }
        public TransactionViewFormSettings TransactionViewFormSettings { get; set; }
        public FormSettings MonaSendFormSettings { get; set; }
        public FormSettings ResponseFormSettings { get; set; }
        public FormSettings ProfileViewFormSettings { get; set; }
        public FormSettings ProfileEditFormSettings { get; set; }
        public FormSettings MonaScatterFormSettings { get; set; }
        public FormSettings TopicCreateFormSettings { get; set; }
        public FormSettings TopicEditFormSettings { get; set; }
        public Options Options { get; set; }

        public ApplicationSettings()
        {
            Account = new Account();
            MainFormSettings = null;
            TransactionViewFormSettings = new TransactionViewFormSettings();
            MonaSendFormSettings = new FormSettings();
            ResponseFormSettings = new FormSettings();
            ProfileViewFormSettings = new FormSettings();
            ProfileEditFormSettings = new FormSettings();
            MonaScatterFormSettings = new FormSettings();
            TopicCreateFormSettings = new FormSettings();
            TopicEditFormSettings = new FormSettings();
            Options = new Options();
        }
    }
}
