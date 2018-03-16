using System;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Settings;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewProfileDialog : FormEx
    {
        private MainForm mParent;
        private Options mOptions;
        private AskMonaApi mApi;
        private int mUserId;

        public ViewProfileDialog(MainForm parent, Options options, AskMonaApi api, int u_id)
        {
            InitializeComponent();
            mParent = parent;
            mOptions = options;
            mApi = api;
            mUserId = u_id;
            button5.Text = "+ " + Common.Digits(options.FirstButtonMona) + " MONA";
            button3.Text = "+ " + Common.Digits(options.SecondButtonMona) + " MONA";
            button4.Text = "+ " + Common.Digits(options.ThirdButtonMona) + " MONA";
            button6.Text = "+ " + Common.Digits(options.ForthButtonMona) + " MONA";
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = !options.AlwaysNonAnonymous;
        }

        private async void ViewProfileDialog_Load(object sender, System.EventArgs e)
        {
            button8.Enabled = mUserId != mApi.UserId;
            var profile = await mApi.FetchUserProfileAsync(mUserId);
            if (profile != null)
            {
                if (profile.Status == 0)
                    MessageBox.Show(profile.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    this.Text = "『" + profile.UserName + profile.UserDan + "』のプロフィール";
                    if (!String.IsNullOrEmpty(profile.Text))
                        textBox2.Text = profile.Text.Replace("\n", "\r\n");
                }
            }
            else
                MessageBox.Show("プロフィールの取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            var balance = await mApi.FetchBlanceAsync(0);
            if (balance != null)
            {
                if (balance.Status == 0)
                    MessageBox.Show(balance.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    textBox4.Text = (double.Parse(balance.Value) / 100000000).ToString("F8");
            }
            else
                MessageBox.Show("残高の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void button1_Click(object sender, System.EventArgs e)
        {
            int sage = checkBox1.Checked ? 1 : 0;
            int anonymous = checkBox2.Checked ? 1 : 0;
            var result = await mApi.SendMonaAsync(mUserId, (ulong)(numericUpDown1.Value * 100000000), anonymous, textBox3.Text, sage);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    MessageBox.Show("送金に成功しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    mParent.UpdateConnectionStatus("通信中");
                    if (!(await mParent.ReloadResponse()))
                        mParent.UpdateConnectionStatus("受信失敗");
                }
            }
            else
                MessageBox.Show("送金に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            var result = await mApi.AddNGUserAsync(mUserId);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    mParent.AddNGUser(mUserId);
                    mParent.UpdateConnectionStatus("通信中");
                    if (!(await mParent.ReloadResponse()))
                        mParent.UpdateConnectionStatus("受信失敗");
                }
            }
            else
                MessageBox.Show("NG ユーザーの追加に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void timer1_Tick(object sender, System.EventArgs e)
        {
            double value, balance;
            double.TryParse(numericUpDown1.Text, out value);
            double.TryParse(textBox4.Text, out balance);
            button1.Enabled = value > 0 && balance >= value;
        }

        private void checkBox2_CheckedChanged(object sender, System.EventArgs e)
        {
            textBox3.ReadOnly = checkBox2.Checked;
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)mOptions.FirstButtonMona;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)mOptions.SecondButtonMona;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)mOptions.ThirdButtonMona;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)mOptions.ForthButtonMona;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value = 0;
        }

        private void ProfileViewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Enabled = false;
        }
    }
}
