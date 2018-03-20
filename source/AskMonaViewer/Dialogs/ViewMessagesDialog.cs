﻿using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewMessagesDialog : FormEx
    {
        public ViewMessagesDialog(string html, string msg, string topicTitle)
        {
            InitializeComponent();
            webBrowser1.DocumentText = html;
            textBox1.Text = msg;
            this.Text = "『" + topicTitle + "』へのレス";
        }
    }
}
