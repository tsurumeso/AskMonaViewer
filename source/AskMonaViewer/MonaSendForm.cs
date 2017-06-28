using System;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class MonaSendForm : Form
    {
        AskMonaApi mApi;
        int mTopicId;
        int mResponseId;

        public MonaSendForm(AskMonaApi api, int t_id, int r_id)
        {
            InitializeComponent();
            mApi = api;
            mTopicId = t_id;
            mResponseId = r_id;
            textBox1.Text = t_id.ToString();
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
                    textBox4.Text = (double.Parse(balance.Value) / 100000000).ToString("F6");
            }
        }

        private async void button1_Click(object sender, System.EventArgs e)
        {
            int sage = checkBox1.Checked ? 1 : 0;
            int anonymous = checkBox2.Checked ? 1 : 0;
            var result = await mApi.SendMonaAsync(mTopicId, mResponseId, (ulong)(numericUpDown1.Value * 100000000), anonymous, textBox3.Text, sage);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("送金に成功しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            button1.Enabled = value > 0 && balance > value;
        }
    }
}
