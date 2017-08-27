using System;
using System.Windows.Forms;

namespace AskMonaViewer
{
    public partial class ProfileEditForm : Form
    {
        private AskMonaApi mApi;

        public ProfileEditForm(AskMonaApi api)
        {
            InitializeComponent();
            mApi = api;
        }

        private async void ProfileEditForm_Load(object sender, EventArgs e)
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

        public FormSettings SaveSettings()
        {
            var settings = new FormSettings();
            settings.Size = this.Size;
            settings.Location = Location;
            return settings;
        }

        public void LoadSettings(FormSettings settings)
        {
            this.Size = settings.Size;
            this.Location = settings.Location;
        }
    }
}
