using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Settings;

namespace AskMonaViewer.Dialogs
{
    public partial class ScatterMonaDialog : FormEx
    {
        private MainForm mParent;
        private Options mOptions;
        private AskMonaApi mApi;
        private Topic mTopic;
        private List<int> mNGUsers;
        private ResponseList mResponseList;

        public ScatterMonaDialog(MainForm parent, Options options, AskMonaApi api, Topic topic, List<int> ngUsers)
        {
            InitializeComponent();
            mParent = parent;
            mOptions = options;
            mApi = api;
            mTopic = topic;
            mNGUsers = ngUsers;
            button5.Text = "+ " + Common.Digits(options.FirstButtonMona) + " MONA";
            button3.Text = "+ " + Common.Digits(options.SecondButtonMona) + " MONA";
            button4.Text = "+ " + Common.Digits(options.ThirdButtonMona) + " MONA";
            button6.Text = "+ " + Common.Digits(options.ForthButtonMona) + " MONA";
            checkBox4.Enabled = topic.ShowHost != 0;
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = !options.AlwaysNonAnonymous;
            this.Text = "『" + topic.Title + "』にばらまく";
        }

        private async void ScatterMonaDialog_Load(object sender, EventArgs e)
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
            if (checkBox8.Checked)
                filteredResponseList = filteredResponseList.Where(x => !mNGUsers.Contains(x.UserId));
            if (checkBox6.Checked)
                filteredResponseList = filteredResponseList.GroupBy(x => x.UserId)
                    .Where(g => g.Count() > 0)
                    .Select(g => g.FirstOrDefault());
            if (checkBox4.Checked)
                filteredResponseList = filteredResponseList.GroupBy(x => x.Host)
                    .Where(g => g.Count() > 0)
                    .Select(g => g.FirstOrDefault());
            if (checkBox7.Checked)
                filteredResponseList = filteredResponseList.Where(x => x.Id >= numericUpDown6.Value && x.Id <= numericUpDown7.Value);
            if (checkBox3.Checked)
                filteredResponseList = filteredResponseList.Where(x => double.Parse(x.Receive) / 100000000 <= (double)numericUpDown3.Value);
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
                mParent.UpdateConnectionStatus("通信中");
                if (!(await mParent.ReloadResponse()))
                    mParent.UpdateConnectionStatus("受信失敗");
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
                numericUpDown4.Value += (decimal)mOptions.FirstButtonMona;
            else
                numericUpDown1.Value += (decimal)mOptions.FirstButtonMona;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)mOptions.SecondButtonMona;
            else
                numericUpDown1.Value += (decimal)mOptions.SecondButtonMona;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)mOptions.ThirdButtonMona;
            else
                numericUpDown1.Value += (decimal)mOptions.ThirdButtonMona;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            if (checkBox5.Checked)
                numericUpDown4.Value += (decimal)mOptions.ForthButtonMona;
            else
                numericUpDown1.Value += (decimal)mOptions.ForthButtonMona;
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
        }

        private void MonaScatterForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Enabled = false;
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            numericUpDown7.Value = numericUpDown6.Value;
        }
    }
}
