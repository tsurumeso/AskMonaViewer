using System;
using System.Drawing;
using System.Windows.Forms;

using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class UploadConfirmationDialog : Form
    {
        private ImgurApi mApi;
        private PostResponseDialog mParent;

        public UploadConfirmationDialog(PostResponseDialog parent, ImgurApi api, Image image)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            pictureBox1.Image = image;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            var result = await mApi.UploadImage(pictureBox1.Image);
            mParent.ImgurImage = result;
            this.Close();
        }
    }
}
