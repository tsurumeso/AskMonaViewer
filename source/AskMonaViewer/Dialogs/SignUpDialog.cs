using System;
using System.Diagnostics;
using System.Windows.Forms;

using AskMonaWrapper;

namespace AskMonaViewer.SubForms
{
    public partial class SignUpDialog : Form
    {
        private Account mAccount;

        public Account Account
        {
            get { return mAccount; }
        }

        public SignUpDialog(Account account)
        {
            InitializeComponent();
            mAccount = account;
            textBox1.Text = account.Address;
            textBox2.Text = account.Password;
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            if (!String.IsNullOrEmpty(textBox4.Text))
                mAccount = new Account().FromAuthCode(textBox4.Text);
            else
                mAccount = new Account(textBox1.Text, textBox2.Text);
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Process.Start(textBox3.Text);
        }
    }
}
