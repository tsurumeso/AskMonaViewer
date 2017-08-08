using System;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class MonaWithdrawForm : Form
    {
        private AskMonaApi mApi;

        public MonaWithdrawForm(AskMonaApi api)
        {
            InitializeComponent();
            mApi = api;
        }

        private async void MonaWithdrawForm_Load(object sender, EventArgs e)
        {
            var balance = await mApi.FetchBlanceAsync(0);
            if (balance != null)
            {
                if (balance.Status == 0)
                    MessageBox.Show("残高の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    textBox4.Text = (double.Parse(balance.Value) / 100000000).ToString("F8");
                    numericUpDown1.Text = textBox4.Text;
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var result = await mApi.WithdrawMonaAsync((ulong)(numericUpDown1.Value * 100000000));
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("出金依頼を送信しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("出金依頼の送信に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            double value, balance;
            double.TryParse(numericUpDown1.Text, out value);
            double.TryParse(textBox4.Text, out balance);
            button1.Enabled = value > 0 && balance >= value;
        }
    }
}
