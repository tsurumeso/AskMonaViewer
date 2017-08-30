using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class MessageViewForm : FormEx
    {
        public MessageViewForm(string html, string msg, string topicTitle)
        {
            InitializeComponent();
            UpdateMessage(html, msg, topicTitle);
        }

        public void UpdateMessage(string html, string msg, string topicTitle)
        {
            webBrowser1.DocumentText = html;
            textBox1.Text = msg;
            this.Text = "『" + topicTitle + "』へのレス";
        }
    }
}
