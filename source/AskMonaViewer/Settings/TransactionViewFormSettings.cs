namespace AskMonaViewer.Settings
{
    public class TransactionViewFormSettings : FormSettings
    {
        public FormSettings MessageViewFormSettings { get; set; }

        public TransactionViewFormSettings()
        {
            MessageViewFormSettings = new FormSettings();
        }
    }
}
