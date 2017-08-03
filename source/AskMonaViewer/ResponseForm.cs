using System;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class ResponseForm : Form
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private int mTopicId;
        private bool mHasCompleted = false;

        public ResponseForm(MainForm parent, AskMonaApi api, int t_id)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mTopicId = t_id;
            button1.Enabled = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (mHasCompleted)
                return;
            else
                mHasCompleted = true;

            int sage = checkBox1.Checked ? 1 : 0;
            var result = await mApi.PostResponseAsync(mTopicId, textBox1.Text, sage);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    await mParent.ReloadResponce(mTopicId);
            }
            else
                MessageBox.Show("レスの投稿に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            button1.Enabled = !String.IsNullOrEmpty(textBox1.Text);
        }
    }
}
