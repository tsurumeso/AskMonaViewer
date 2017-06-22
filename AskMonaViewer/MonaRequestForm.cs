using System;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class MonaRequestForm : Form
    {
        AskMonaApi mApi;
        int mTopicId;
        int mResponseId;

        public MonaRequestForm(AskMonaApi api, int t_id, int r_id)
        {
            InitializeComponent();
            mApi = api;
            mTopicId = t_id;
            mResponseId = r_id;
            textBox1.Text = t_id.ToString();
            textBox2.Text = r_id.ToString();
            button1.Enabled = false;
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
            var result = await mApi.SendMonaAsync(mTopicId, mResponseId, (ulong)(numericUpDown1.Value * 1000000000), 1, textBox3.Text, sage);
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

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            button1.Enabled = numericUpDown1.Value > 0;
        }
    }
}
