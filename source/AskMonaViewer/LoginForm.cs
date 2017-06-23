using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class LoginForm : Form
    {
        MainForm mParentForm;

        public LoginForm(MainForm parent)
        {
            InitializeComponent();
            mParentForm = parent;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            mParentForm.SetAccount(textBox1.Text, textBox2.Text);
            this.Close();
        }
    }
}
