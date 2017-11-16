using AskMonaWrapper;

namespace AskMonaViewer.Settings
{
    public class ApplicationSettings
    {
        public Account Account { get; set; }
        public MainFormSettings MainFormSettings { get; set; }
        public DialogSettings ViewTransactionDialogSettings { get; set; }
        public DialogSettings ViewMessageDialogSettings { get; set; }
        public DialogSettings SendMonaDialogSettings { get; set; }
        public DialogSettings PostResponseDialogSettings { get; set; }
        public DialogSettings ViewProfileDialogSettings { get; set; }
        public DialogSettings EditProfileDialogSettings { get; set; }
        public DialogSettings ScatterMonaDialogSettings { get; set; }
        public DialogSettings CreateTopicDialogSettings { get; set; }
        public DialogSettings EditTopicDialogSettings { get; set; }
        public DialogSettings ViewimgurDialogSettings { get; set; }
        public Options Options { get; set; }

        public ApplicationSettings()
        {
            Account = new Account();
            MainFormSettings = new MainFormSettings();
            ViewTransactionDialogSettings = new DialogSettings();
            ViewMessageDialogSettings = new DialogSettings();
            SendMonaDialogSettings = new DialogSettings();
            PostResponseDialogSettings = new DialogSettings();
            ViewProfileDialogSettings = new DialogSettings();
            EditProfileDialogSettings = new DialogSettings();
            ScatterMonaDialogSettings = new DialogSettings();
            CreateTopicDialogSettings = new DialogSettings();
            EditTopicDialogSettings = new DialogSettings();
            ViewimgurDialogSettings = new DialogSettings();
            Options = new Options();
        }
    }
}
