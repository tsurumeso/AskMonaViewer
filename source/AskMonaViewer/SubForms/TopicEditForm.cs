using System;
using System.Linq;
using System.Windows.Forms;

using AskMonaViewer.Api;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class TopicEditForm : FormEx
    {
        private AskMonaApi mApi;
        private Topic mTopic;

        public TopicEditForm(AskMonaApi api, Topic topic)
        {
            InitializeComponent();
            mApi = api;
            mTopic = topic;
            this.Text = "『" + topic.Title + "』の編集";
        }

        private void TopicEditForm_Load(object sender, EventArgs e)
        {
            if (mTopic.Lead != null)
                textBox2.Text = mTopic.Lead.Replace("\n", "\r\n");
            if (mTopic.Supplyment != null)
                textBox3.Text = mTopic.Supplyment.Replace("\n", "\r\n");
            textBox4.Text = mTopic.Tags;
            comboBox1.SelectedIndex = mTopic.CategoryId;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var array = textBox4.Text.Split(null);
            if (array.Count() > 5 || array.Any(x => x.Length > 12))
            {
                MessageBox.Show("1 つのタグは 12 字以内で 5 つまで登録可能です", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = await mApi.EditTopicAsync(mTopic.Id, comboBox1.SelectedIndex, textBox4.Text, textBox2.Text, textBox3.Text);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("トピックの編集に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
