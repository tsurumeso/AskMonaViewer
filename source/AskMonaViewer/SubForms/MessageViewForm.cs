using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class MessageViewForm : FormEx
    {
        public MessageViewForm(string html, string msg, string topicTitle)
        {
            InitializeComponent();
            webBrowser1.DocumentText = html;
            textBox1.Text = msg;
            this.Text = "『" + topicTitle + "』へのレス";
        }
    }
}
