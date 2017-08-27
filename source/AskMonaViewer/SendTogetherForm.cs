using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class SendTogetherForm : Form
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private Topic mTopic;
        private ResponseList mResponseList;

        public SendTogetherForm(MainForm parent, AskMonaApi api, Topic topic)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mTopic = topic;
            this.Text = "『" + topic.Title + "』にばらまく";
        }

        private async void SendTogetherForm_Load(object sender, EventArgs e)
        {
            mResponseList = await mApi.FetchResponseListAsync(mTopic.Id);
            textBox2.Text = FilterResponseList(mResponseList.Responses).Count().ToString();
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

        private IEnumerable<Response> FilterResponseList(List<Response> responseList)
        {
            var filteredResponseList = responseList.Where(x => x.UserId != mApi.UserId);
            if (checkBox3.Enabled && checkBox3.Checked)
                filteredResponseList = filteredResponseList.Where(x => double.Parse(x.Receive) / 100000000 <= (double)numericUpDown3.Value);
            if (checkBox4.Enabled && checkBox4.Checked)
                filteredResponseList = filteredResponseList.Where(x => IntegerUserTimes(x.UserTimes) <= (double)numericUpDown2.Value);
            return filteredResponseList;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            bool flag = true;
            int sage = checkBox1.Checked ? 1 : 0;
            int anonymous = checkBox2.Checked ? 1 : 0;
            var responseList = FilterResponseList(mResponseList.Responses);

            foreach (var response in responseList)
            {
                ulong value = 0;
                if (checkBox5.Checked)
                {
                    var receive = double.Parse(response.Receive) / 100000000;
                    if (receive < (double)numericUpDown4.Value)
                        value = (ulong)(((double)numericUpDown4.Value - receive) * 100000000);
                }
                else
                    value = (ulong)(numericUpDown1.Value * 100000000);

                if (value == 0)
                    continue;

                var result = await mApi.SendMonaAsync(mTopic.Id, response.Id, value, anonymous, textBox3.Text, sage);
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
            {
                MessageBox.Show("送金に成功しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await mParent.ReloadResponce();
            }

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
            var responseList = FilterResponseList(mResponseList.Responses);

            int count = 0;
            double sumValue = 0;
            if (checkBox5.Checked)
            {
                foreach (var response in responseList)
                {
                    var receive = double.Parse(response.Receive) / 100000000;
                    if (receive < (double)numericUpDown4.Value)
                    {
                        sumValue += (double)numericUpDown4.Value - receive;
                        count++;
                    }
                }
            }
            else
            {
                count = responseList.Count();
                sumValue = value * count;
            }

            textBox1.Text = sumValue.ToString("F8");
            textBox2.Text = count.ToString();
            button1.Enabled = sumValue > 0 && balance >= sumValue;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)0.3939;
            else
                numericUpDown1.Value += (decimal)0.3939;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)0.003939;
            else
                numericUpDown1.Value += (decimal)0.003939;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)0.114114;
            else
                numericUpDown1.Value += (decimal)0.114114;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)0.00114114;
            else
                numericUpDown1.Value += (decimal)0.00114114;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value = 0;
            else
                numericUpDown1.Value = 0;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox3.ReadOnly = checkBox2.Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown1.Enabled = !checkBox5.Checked;
            groupBox1.Enabled = !checkBox5.Checked;
        }
    }
}
