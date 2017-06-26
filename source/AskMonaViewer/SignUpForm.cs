using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class SignUpForm : Form
    {
        MainForm mParentForm;

        public SignUpForm(MainForm parent)
        {
            InitializeComponent();
            mParentForm = parent;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox4.Text))
                mParentForm.SetAccount(textBox4.Text);
            else
                mParentForm.SetAccount(textBox1.Text, textBox2.Text);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start(textBox3.Text);
        }
    }
}
