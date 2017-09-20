using System;
using System.Linq;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class TopicEditForm : FormEx
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private Topic mTopic;

        public TopicEditForm(MainForm parent, AskMonaApi api, Topic topic)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mTopic = topic;
            this.Text = "『" + topic.Title + "』の編集";

            if (topic.UserId != api.UserId)
            {
                textBox2.ReadOnly = true;
                textBox3.ReadOnly = true;
                checkBox1.Enabled = false;
                comboBox2.Enabled = false;

                if (topic.Editable == 0)
                {
                    textBox4.ReadOnly = true;
                    comboBox1.Enabled = false;
                }
            }
        }

        private void TopicEditForm_Load(object sender, EventArgs e)
        {
            if (mTopic.Lead != null)
                textBox2.Text = mTopic.Lead.Replace("\n", "\r\n");
            if (mTopic.Supplyment != null)
                textBox3.Text = mTopic.Supplyment.Replace("\n", "\r\n");
            textBox4.Text = mTopic.Tags;
            checkBox1.Checked = mTopic.Editable == 1;
            comboBox1.SelectedIndex = mTopic.CategoryId;
            comboBox2.SelectedIndex = mTopic.ShowHost;
        }

        private Topic EditTopic(Topic src)
        {
            var topic = new Topic();

            topic.Id = src.Id;
            topic.CategoryId = comboBox1.SelectedIndex == src.CategoryId ? -1 : comboBox1.SelectedIndex;
            topic.Tags = src.UserId != mApi.UserId ? (src.Editable == 0 ? null : textBox4.Text) : textBox4.Text;
            topic.Editable = src.UserId != mApi.UserId ? -1 : (checkBox1.Checked ? 1 : 0);
            topic.ShowHost = comboBox2.SelectedIndex == src.ShowHost ? -1 : comboBox2.SelectedIndex;
            topic.Lead = src.Lead;
            if (topic.Lead != null)
                topic.Lead = textBox2.Text == src.Lead.Replace("\n", "\r\n") ? null : textBox2.Text;
            topic.Supplyment = src.Supplyment;
            if (topic.Supplyment != null)
                topic.Supplyment = textBox3.Text == src.Supplyment.Replace("\n", "\r\n") ? null : textBox3.Text;

            return topic;
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            var array = textBox4.Text.Split(null);
            if (array.Count() > 5 || array.Any(x => x.Length > 12))
            {
                MessageBox.Show("1 つのタグは 12 字以内で 5 つまで登録可能です", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = await mApi.EditTopicAsync(EditTopic(mTopic));
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    mParent.UpdateConnectionStatus("通信中");
                    if (!(await mParent.ReloadResponse()))
                        mParent.UpdateConnectionStatus("受信失敗");
                }
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
