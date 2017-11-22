using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewMessageDialog : FormEx
    {
        public ViewMessageDialog(string html, string msg, string topicTitle)
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
