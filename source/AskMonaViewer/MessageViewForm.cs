using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class MessageViewForm : Form
    {
        public MessageViewForm(string html, string msg)
        {
            InitializeComponent();
            webBrowser1.DocumentText = html;
            textBox1.Text = msg;
        }
    }
}
