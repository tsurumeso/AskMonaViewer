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
            var result = await mApi.FetchNGUsersAsync();
            if (result != null)
            {
                if (result.Status == 0)
                    MessageBox.Show(result.Error, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                {
                    listViewEx1.BeginUpdate();
                    foreach (var ngUser in result.Users)
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
            else
                MessageBox.Show("情報の取得に失敗しました", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private async void Delete_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

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

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Delete_ToolStripMenuItem.Enabled = listViewEx1.SelectedItems.Count != 0;
        }
    }
}
