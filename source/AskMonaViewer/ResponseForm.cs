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

        private int DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();
            return (int)(dateTime - unixEpoch).TotalSeconds;
        }

        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var profile = await mApi.EditUserProfileAsync();
            if (profile == null)
                return;

            var response = new Response();
            var responseList = new ResponseList();

            response.Id = 1;
            response.UserId = mApi.UserId;
            response.UserName = profile.UserName;
            response.UserDan = profile.UserDan;
            response.Created = DateTimeToUnixTimeStamp(DateTime.Now);
            response.UserTimes = "1/1";
            response.Receive = "0";
            response.ReceivedCount = 0;
            response.Level = 0;
            response.Text = textBox1.Text;
            responseList.Topic = new Topic();
            responseList.Topic.Id = mTopicId;
            responseList.Responses.Add(response);

            webBrowser1.DocumentText = await mParent.BuildWebBrowserDocument(responseList);
        }
    }
}
