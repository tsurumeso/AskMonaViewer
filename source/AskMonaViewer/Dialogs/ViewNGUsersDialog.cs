using System;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewNGUsersDialog : FormEx
    {
        private MainForm mParent;
        private AskMonaApi mApi;

        public ViewNGUsersDialog(MainForm parent, AskMonaApi api)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
        }

        private async void ViewNGUsersDialog_Load(object sender, EventArgs e)
        {
            var ngUsers = await mApi.FetchNGUsersAsync();
            if (ngUsers != null)
            {
                if (ngUsers.Status == 0)
                    MessageBox.Show("NG ユーザーの一覧の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    listViewEx1.BeginUpdate();
                    foreach (var ngUser in ngUsers.Users)
                    {
                        var lvi = new ListViewItem(
                            new string[] {
                                ngUser.UserId.ToString(),
                                ngUser.UserName + ngUser.UserDan,
                            }
                        );
                        lvi.Tag = ngUser;
                        listViewEx1.Items.Add(lvi);
                    }
                    listViewEx1.EndUpdate();
                }
            }
        }

        private async void Delete_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ngUser = (User)listViewEx1.SelectedItems[0].Tag;
            var result = await mApi.DeleteNGUserAsync(ngUser.UserId);
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    mParent.DeleteNGUser(ngUser.UserId);
                    mParent.UpdateConnectionStatus("通信中");
                    if (!(await mParent.ReloadResponse()))
                        mParent.UpdateConnectionStatus("受信失敗");
                    listViewEx1.Items.RemoveAt(listViewEx1.SelectedIndices[0]);
                }
            }
            else
                MessageBox.Show("NG ユーザーの削除に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
