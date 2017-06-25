using System;
using System.Linq;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class TopicCreateForm : Form
    {
        private AskMonaApi mApi;

        public TopicCreateForm(AskMonaApi api)
        {
            InitializeComponent();
            mApi = api;
            button2.Enabled = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var array = textBox3.Text.Split(null);
            if (array.Count() > 5 || array.Any(x => x.Length > 12))
            {
                MessageBox.Show("1 つのタグは 12 字以内で 5 つまで登録可能です", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = await mApi.CreateTopicAsync(
                Uri.EscapeUriString(textBox1.Text), 
                Uri.EscapeUriString(textBox2.Text), 
                comboBox1.SelectedIndex, 
                Uri.EscapeUriString(textBox3.Text));

            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show("トピックの作成に成功しました", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MessageBox.Show("トピックの作成に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = !String.IsNullOrEmpty(textBox1.Text) && !String.IsNullOrEmpty(textBox2.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            button2.Enabled = !String.IsNullOrEmpty(textBox1.Text) && !String.IsNullOrEmpty(textBox2.Text);
        }
    }
}
