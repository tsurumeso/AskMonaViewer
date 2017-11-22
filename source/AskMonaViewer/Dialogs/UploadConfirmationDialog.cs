using System;
using System.Drawing;
using System.Windows.Forms;

using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class UploadConfirmationDialog : Form
    {
        private ImgurApi mApi;
        private ImgurImage mImgurImage;

        public ImgurImage ImgurImage
        {
            get { return mImgurImage; }
        }

        public UploadConfirmationDialog(ImgurApi api, Image image)
        {
            InitializeComponent();
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
            mImgurImage = result;
            this.Close();
        }
    }
}
