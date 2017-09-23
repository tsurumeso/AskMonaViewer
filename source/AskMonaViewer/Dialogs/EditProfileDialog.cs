using System;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class EditProfileDialog : FormEx
    {
        private AskMonaApi mApi;

        public EditProfileDialog(AskMonaApi api)
        {
            InitializeComponent();
            mApi = api;
        }

        private async void EditProfileDialog_Load(object sender, EventArgs e)
        {
            var profile = await mApi.EditUserProfileAsync();
            if (profile == null)
                return;

            textBox1.Text = profile.UserName;
            if (profile.Text != null)
                textBox2.Text = profile.Text.Replace("\\n", Environment.NewLine);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var result = await mApi.EditUserProfileAsync(textBox1.Text, textBox2.Text);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
                MessageBox.Show("マイプロフィールの変更に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);

            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
