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

        public FormSettings SaveSettings()
        {
            var settings = new FormSettings();
            settings.Size = this.Size;
            settings.Location = Location;
            return settings;
        }

        public void LoadSettings(FormSettings settings)
        {
            this.Size = settings.Size;
            this.Location = settings.Location;
        }
    }
}
