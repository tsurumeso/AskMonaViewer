using AskMonaViewer.Utilities;

namespace AskMonaViewer
{
    public class Settings
    {
        public Account Account { get; set; }
        public MainFormSettings MainFormSettings { get; set; }
        public TransactionViewFormSettings TransactionViewFormSettings { get; set; }
        public FormSettings MonaSendFormSettings { get; set; }
        public FormSettings ResponseFormSettings { get; set; }
        public FormSettings ProfileEditFormSettings { get; set; }
        public FormSettings MonaScatterFormSettings { get; set; }
        public FormSettings TopicCreateFormSettings { get; set; }
        public FormSettings TopicEditFormSettings { get; set; }

        public Settings()
        {
            Account = new Account();
            MainFormSettings = null;
            TransactionViewFormSettings = new TransactionViewFormSettings();
            MonaSendFormSettings = new FormSettings();
            ResponseFormSettings = new FormSettings();
            ProfileEditFormSettings = new FormSettings();
            MonaScatterFormSettings = new FormSettings();
            TopicCreateFormSettings = new FormSettings();
            TopicEditFormSettings = new FormSettings();
        }
    }

    public class MainFormSettings : FormSettings
    {
        public bool IsHorizontal { get; set; }
        public int VSplitterDistance { get; set; }
        public int HSplitterDistance { get; set; }
    }

    public class TransactionViewFormSettings : FormSettings
    {
        public FormSettings MessageViewFormSettings { get; set; }

        public TransactionViewFormSettings()
        {
            MessageViewFormSettings = new FormSettings();
        }
    }
}
