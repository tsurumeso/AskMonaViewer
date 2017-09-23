namespace AskMonaViewer.Settings
{
    public class ViewTransactionDialogSettings : DialogSettings
    {
        public DialogSettings ViewMessageDialogSettings { get; set; }

        public ViewTransactionDialogSettings()
        {
            ViewMessageDialogSettings = new DialogSettings();
        }
    }
}
