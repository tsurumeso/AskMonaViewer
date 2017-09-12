using System;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Settings;

namespace AskMonaViewer.SubForms
{
    public partial class MonaSendForm : FormEx
    {
        private MainForm mParent;
        private Options mOptions;
        private AskMonaApi mApi;
        private Topic mTopic;
        private int mResponseId;

        public MonaSendForm(MainForm parent, Options options, AskMonaApi api, Topic topic, int r_id)
        {
            InitializeComponent();
            mParent = parent;
            mOptions = options;
            mApi = api;
            mTopic = topic;
            mResponseId = r_id;
            button5.Text = "+ " + Common.Digits(options.FirstButtonMona) + " MONA";
            button3.Text = "+ " + Common.Digits(options.SecondButtonMona) + " MONA";
            button4.Text = "+ " + Common.Digits(options.ThirdButtonMona) + " MONA";
            button6.Text = "+ " + Common.Digits(options.ForthButtonMona) + " MONA";
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = !options.AlwaysNonAnonymous;
            this.Text = "『" + topic.Title + "』に送る";
            textBox2.Text = r_id.ToString();
        }

        private async void MonaRequestForm_Load(object sender, System.EventArgs e)
        {
            var balance = await mApi.FetchBlanceAsync(0);
            if (balance != null)
            {
                if (balance.Status == 0)
                    MessageBox.Show("残高の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    textBox4.Text = (double.Parse(balance.Value) / 100000000).ToString("F8");
            }
        }

        private async void button1_Click(object sender, System.EventArgs e)
        {
            int sage = checkBox1.Checked ? 1 : 0;
            int anonymous = checkBox2.Checked ? 1 : 0;
            var result = await mApi.SendMonaAsync(mTopic.Id, mResponseId, (ulong)(numericUpDown1.Value * 100000000), anonymous, textBox3.Text, sage);
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

        private void button2_Click(object sender, System.EventArgs e)
        {
            this.Close();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.ReadOnly = checkBox2.Checked;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double value, balance;
            double.TryParse(numericUpDown1.Text, out value);
            double.TryParse(textBox4.Text, out balance);
            button1.Enabled = value > 0 && balance >= value;
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

        private void MonaSendForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Enabled = false;
        }
    }
}
