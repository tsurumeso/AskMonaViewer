using System;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Settings;
using System.Drawing;

namespace AskMonaViewer.Dialogs
{
    public partial class PostResponseDialog : FormEx
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private Topic mTopic;
        private ImgurApi mImgurApi;
        private bool mHasCompleted = false;

        public PostResponseDialog(MainForm parent, Options options, AskMonaApi api, ImgurApi imgurApi, Topic topic)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mTopic = topic;
            mImgurApi = imgurApi;
            checkBox1.Checked = options.AlwaysSage;
            this.Text = "『" + topic.Title + "』にレス";
            button1.Enabled = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (mHasCompleted)
                return;
            else
                mHasCompleted = true;

            int sage = checkBox1.Checked ? 1 : 0;
            var result = await mApi.PostResponseAsync(mTopic.Id, textBox1.Text, sage);
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
                MessageBox.Show("レスの投稿に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        public void UpdateTopic(Topic topic)
        {
            mTopic = topic;
            this.Text = "『" + topic.Title + "』にレス";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = String.Format("Length: {0}/1024", textBox1.Text.Length);
            toolStripStatusLabel2.Text = String.Format("Lines: {0}/15", textBox1.Lines.Length);
            button1.Enabled = !String.IsNullOrEmpty(textBox1.Text) && textBox1.Lines.Length <= 15;
        }

        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var profile = await mApi.EditUserProfileAsync();
            if (profile == null)
                return;

            var response = new Response();
            var responseList = new ResponseList();

            response.Id = mTopic.Count + 1;
            response.UserId = mApi.UserId;
            response.UserName = profile.UserName;
            response.UserDan = profile.UserDan;
            response.Created = Common.DateTimeToUnixTimeStamp(DateTime.Now);
            response.UserTimes = "1/1";
            response.Receive = "0";
            response.ReceivedCount = 0;
            response.Level = 0;
            response.Text = textBox1.Text;
            responseList.Topic = mTopic;
            responseList.Responses.Add(response);

            webBrowser1.DocumentText = await mParent.BuildWebBrowserDocument(responseList);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "アップロードするファイルを選択してください";
            ofd.Filter = "すべてのファイル(*.*)|*.*";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                var image = Image.FromFile(ofd.FileName);
                var uploadConfirmationDialog = new UploadConfirmationDialog(mImgurApi, image);
                uploadConfirmationDialog.ShowDialog();

                var imgurImage = uploadConfirmationDialog.ImgurImage;
                if (imgurImage == null)
                {
                    MessageBox.Show("画像のアップロードに失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                textBox1.Text += "\r\n" + imgurImage.Link;
                mParent.AddImgurImage(imgurImage);
            }
        }
    }
}
