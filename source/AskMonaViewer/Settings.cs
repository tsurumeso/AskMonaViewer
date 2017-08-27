using System;
using System.Drawing;
using System.Windows.Forms;

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
        public FormSettings SendTogetherFormSettings { get; set; }
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
            SendTogetherFormSettings = new FormSettings();
            TopicCreateFormSettings = new FormSettings();
            TopicEditFormSettings = new FormSettings();
        }
    }

    public class FormSettings
    {
        public Size Size { get; set; }
        public Point Location { get; set; }

        public FormSettings()
        {
            Size = new Size(0, 0);
            Location = new Point(0, 0);
        }
    }

    public class MainFormSettings : FormSettings
    {
        public FormWindowState WindowState { get; set; }
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
