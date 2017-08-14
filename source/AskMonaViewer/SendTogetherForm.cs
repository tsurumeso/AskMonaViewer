using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class SendTogetherForm : Form
    {
        private AskMonaApi mApi;
        private Topic mTopic;
        private ResponseList mResponseList;

        public SendTogetherForm(AskMonaApi api, Topic topic)
        {
            InitializeComponent();
            mApi = api;
            mTopic = topic;
            textBox5.Text = topic.Title;
        }

        private async void SendTogetherForm_Load(object sender, EventArgs e)
        {
            mResponseList = await mApi.FetchResponseListAsync(mTopic.Id);
            textBox2.Text = mResponseList.Responses.Where(x => x.UserId != mApi.UserId)
                .Where(x => double.Parse(x.Receive) / 100000000 <= (double)numericUpDown3.Value)
                .Where(x => IntegerUserTimes(x.UserTimes) <= (double)numericUpDown2.Value).Count().ToString();
            timer1.Enabled = true;

            var balance = await mApi.FetchBlanceAsync(0);
            if (balance != null)
            {
                if (balance.Status == 0)
                    MessageBox.Show("残高の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    textBox4.Text = (double.Parse(balance.Value) / 100000000).ToString("F8");
            }
        }

        private static int IntegerUserTimes(string userTimes)
        {
            return int.Parse(userTimes.Substring(userTimes.IndexOf("/") + 1));
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            bool flag = true;
            int sage = checkBox1.Checked ? 1 : 0;
            int anonymous = checkBox2.Checked ? 1 : 0;
            var responseList = mResponseList.Responses.Where(x => x.UserId != mApi.UserId)
                .Where(x => double.Parse(x.Receive) / 100000000 <= (double)numericUpDown3.Value)
                .Where(x => IntegerUserTimes(x.UserTimes) <= (double)numericUpDown2.Value).ToList();

            foreach (var response in responseList)
            {
                var result = await mApi.SendMonaAsync(mTopic.Id, response.Id, (ulong)(numericUpDown1.Value * 100000000), anonymous, textBox3.Text, sage);
                if (result != null)
                {
                    if (result.Status == 0)
                    {
                        MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        flag = false;
                        break;
                    }
                }
                else
                {
                    MessageBox.Show("送金に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    flag = false;
                    break;
                }
            }
            if (flag)
                MessageBox.Show("送金に成功しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double value, balance;
            double.TryParse(numericUpDown1.Text, out value);
            double.TryParse(textBox4.Text, out balance);
            var count = mResponseList.Responses.Where(x => x.UserId != mApi.UserId)
                .Where(x => double.Parse(x.Receive) / 100000000 <= (double)numericUpDown3.Value)
                .Where(x => IntegerUserTimes(x.UserTimes) <= (double)numericUpDown2.Value).Count();
            var trueValue = value * count;
            textBox1.Text = trueValue.ToString("F8");
            textBox2.Text = count.ToString();
            button1.Enabled = trueValue > 0 && balance >= trueValue;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)0.3939;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)0.003939;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)0.114114;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value += (decimal)0.00114114;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            numericUpDown1.Value = 0;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.ReadOnly = checkBox2.Checked;
        }
    }
}
