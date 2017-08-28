using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class MessageViewForm : FormEx
    {
        public MessageViewForm(string html, string msg)
        {
            InitializeComponent();
            webBrowser1.DocumentText = html;
            textBox1.Text = msg;
        }
    }
}
