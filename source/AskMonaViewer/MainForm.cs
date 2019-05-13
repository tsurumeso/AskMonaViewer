using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Dialogs;
using AskMonaViewer.Settings;

namespace AskMonaViewer
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            mListViewItemSorter = new ListViewItemComparer();
            mListViewItemSorter.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.Double,
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.Double,
                ListViewItemComparer.ComparerMode.DateTime
            };
            mNGUsers = new List<int>();
            mTopicList = new List<Topic>();
            mFavoriteTopicList = new List<Topic>();
            mResponseCache = new List<ResponseList>();
            mImgurImageList = new List<ImgurImage>();
            mSettings = new ApplicationSettings();
            mImgurApi = new ImgurApi("");
            mZaifApi = new ZaifApi();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadHtmlHeader();

            mAskMonaApi = new AskMonaApi("3738", "", mSettings.Account);
            if (await mAskMonaApi.VerifySecretKeyAsync() == null)
            {
                var signUpDialog = new SignUpDialog(mSettings.Account);
                signUpDialog.ShowDialog();
                mAskMonaApi.Account = signUpDialog.Account;
                mSettings.Account = signUpDialog.Account;
            }

            var topicList = await mAskMonaApi.FetchFavoriteTopicListAsync();
            if (topicList != null)
                mFavoriteTopicList = topicList.Topics;

            var ngUsers = await mAskMonaApi.FetchNGUsersAsync();
            if (ngUsers != null)
                mNGUsers = ngUsers.Users.Select(x => x.UserId).ToList<int>();

            foreach (var topicId in mSettings.MainFormSettings.TabTopicList)
            {
                UpdateConnectionStatus("通信中");
                toolStripComboBox1.Text = "https://askmona.org/" + topicId;
                if (await InitializeTabPage(topicId))
                    UpdateConnectionStatus("受信完了");
                else
                    UpdateConnectionStatus("受信失敗");
            }

            UpdateConnectionStatus("通信中");
            if (await UpdateTopicList(mCategoryId))
                UpdateConnectionStatus("受信完了");
            else
                UpdateConnectionStatus("受信失敗");

            tabControl1.SelectedIndex = mSettings.MainFormSettings.SelectedTabIndex;
            if (tabControl1.SelectedIndex == 0)
                tabControl1_SelectedIndexChanged(this, new EventArgs());

            await UpdateCurrencyPrice();
            EnableControls();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            SaveSettings();
        }

        private async void listView1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var itemIndex = listView1.SelectedIndices[0];
            var topic = (Topic)listView1.Items[itemIndex].Tag;
            UpdateConnectionStatus("通信中");
            toolStripComboBox1.Text = "https://askmona.org/" + topic.Id;
            if (!(await UpdateResponse(topic.Id)))
                UpdateConnectionStatus("受信失敗");

            if (mPostResponseDialog != null)
                mPostResponseDialog.UpdateTopic(topic);
            if (mScatterMonaDialog != null)
            {
                var idx = mResponseCache.FindIndex(x => x.Topic.Id == mTopic.Id);
                if (idx == -1)
                    return;
                mScatterMonaDialog.UpdateTopic(mResponseCache[idx]);
            }

            var topicIndex = mTopicList.FindIndex(x => x.Id == topic.Id);
            if (topicIndex != -1)
                mTopicList[topicIndex].CachedCount = mTopic.Count;

            listView1.Items[itemIndex].SubItems[4].Text = mTopic.Count.ToString();
            listView1.Items[itemIndex].SubItems[5].Text = "";
            UpdateFavoriteToolStrip();
        }

        private async void Document_Click(object sender, HtmlElementEventArgs e)
        {
            string link = null;
            HtmlElement clickedElement = mPrimaryWebBrowser.Document.GetElementFromPoint(e.MousePosition);
            if (clickedElement == null)
                return;
            else if (clickedElement.TagName.ToLower() == "a")
                link = clickedElement.GetAttribute("href");
            else if (clickedElement.Parent != null)
            {
                if (clickedElement.Parent.TagName.ToLower() == "a")
                    link = clickedElement.Parent.GetAttribute("href");
            }

            if (String.IsNullOrEmpty(link))
                return;
            else
                e.ReturnValue = false;

            await ExecuteLink(link);
        }

        private async void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                mCategoryId = int.Parse(listView2.SelectedItems[0].Tag.ToString());
                mTopicList.Clear();
                listView1.Items.Clear();
                UpdateConnectionStatus("通信中");
                if (await UpdateTopicList(mCategoryId))
                    UpdateConnectionStatus("受信完了");
                else
                    UpdateConnectionStatus("受信失敗");
            }
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            // イベント削除から登録で常に一つだけ登録されるようにする
            mPrimaryWebBrowser.Document.Click -= new HtmlElementEventHandler(Document_Click);
            mPrimaryWebBrowser.Document.ContextMenuShowing -= new HtmlElementEventHandler(Document_ContextMenuShowing);
            mPrimaryWebBrowser.Document.Window.DetachEventHandler("onscroll", OnScrollEventHandler);

            mPrimaryWebBrowser.Document.Click += new HtmlElementEventHandler(Document_Click);
            mPrimaryWebBrowser.Document.ContextMenuShowing += new HtmlElementEventHandler(Document_ContextMenuShowing);
            mPrimaryWebBrowser.Document.Window.AttachEventHandler("onscroll", OnScrollEventHandler);

            if (mIsDocumentLoading)
                mPrimaryWebBrowser.Document.Window.ScrollTo(new Point(0, mTopic.Scrolled));

            mIsDocumentLoading = false;
            UpdateConnectionStatus("受信完了");
        }

        private void Document_ContextMenuShowing(object sender, HtmlElementEventArgs e)
        {
            var doc = (mshtml.IHTMLDocument2)mPrimaryWebBrowser.Document.DomDocument;
            var range = (mshtml.IHTMLTxtRange)doc.selection.createRange();
            var enabled = !String.IsNullOrEmpty(range.text);
            Copy_ToolStripMenuItem.Enabled = enabled;
            Search_ToolStripMenuItem.Enabled = enabled;
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorter.Column = e.Column;
            listView1.Sort();
            Common.UpdateColumnColors(listView1, Color.White, Color.Lavender);
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            FilterTopics(comboBox1.Text);
        }

        private async void toolStripButton3_Click(object sender, EventArgs e)
        {
            mTopicList.Clear();
            listView1.Items.Clear();
            UpdateConnectionStatus("通信中");
            if (await UpdateTopicList(mCategoryId))
                UpdateConnectionStatus("受信完了");
            else
                UpdateConnectionStatus("受信失敗");
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(toolStripComboBox1.Text))
                System.Diagnostics.Process.Start(toolStripComboBox1.Text);
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            toolStripButton4.Checked = true;
            toolStripButton5.Checked = false;
            splitContainer1.Orientation = Orientation.Horizontal;
            splitContainer1.SplitterDistance = 267;
        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            toolStripButton4.Checked = false;
            toolStripButton5.Checked = true;
            splitContainer1.Orientation = Orientation.Vertical;
            splitContainer1.SplitterDistance = 720;
        }

        private async void timer1_Tick(object sender, EventArgs e)
        {
            await UpdateCurrencyPrice();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (mTopic == null || mPostResponseDialog != null)
                return;

            mPostResponseDialog = new PostResponseDialog(this, mSettings.Options, mAskMonaApi, mImgurApi, mTopic);
            mPostResponseDialog.LoadSettings(mSettings.PostResponseDialogSettings);
            mPostResponseDialog.FormClosed += OnPostResponseDialogClosed;
            mPostResponseDialog.Show(this);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var createTopicDialog = new CreateTopicDialog(mAskMonaApi);
            createTopicDialog.LoadSettings(mSettings.CreateTopicDialogSettings);
            createTopicDialog.ShowDialog();
            mSettings.CreateTopicDialogSettings = createTopicDialog.SaveSettings();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            var withdrawMonaDialog = new WithdrawMonaDialog(mAskMonaApi);
            withdrawMonaDialog.ShowDialog();
        }

        private async void toolStripButton7_Click(object sender, EventArgs e)
        {
            var deposit = await mAskMonaApi.FetchDepositAddressAsync();
            Clipboard.SetText(deposit.Address);
        }

        private void Copy_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mPrimaryWebBrowser.Document.ExecCommand("Copy", false, null);
        }

        private void SelectAll_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mPrimaryWebBrowser.Document.ExecCommand("SelectAll", false, null);
        }

        private void Search_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var doc = (mshtml.IHTMLDocument2)mPrimaryWebBrowser.Document.DomDocument;
            var range = (mshtml.IHTMLTxtRange)doc.selection.createRange();
            var url = "https://www.google.co.jp/search?q=" + System.Net.WebUtility.UrlEncode(range.text);
            System.Diagnostics.Process.Start(url);
        }

        private async void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            int idx = mFavoriteTopicList.FindIndex(x => x.Id == mTopic.Id);
            if (idx == -1)
            {
                await mAskMonaApi.AddFavoriteTopicAsync(mTopic.Id);
                mFavoriteTopicList.Add(mTopic);
            }
            else
            {
                await mAskMonaApi.DeleteFavoriteTopicAsync(mTopic.Id);
                mFavoriteTopicList.RemoveAt(idx);
            }

            mTopicList.Clear();
            listView1.Items.Clear();
            UpdateConnectionStatus("通信中");
            if (await UpdateTopicList(mCategoryId))
                UpdateConnectionStatus("受信完了");
            else
                UpdateConnectionStatus("受信失敗");
            UpdateFavoriteToolStrip();
        }

        private async void toolStripButton15_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            UpdateConnectionStatus("通信中");
            if (!(await UpdateResponse(mTopic.Id)))
                UpdateConnectionStatus("受信失敗");
        }

        private async void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var res = MessageBox.Show("トピックのキャッシュを削除して再度読み込みます\nよろしいですか？", "確認",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);
            if (res == DialogResult.Yes)
            {
                UpdateConnectionStatus("通信中");
                if (!(await ReloadResponse()))
                    UpdateConnectionStatus("受信失敗");
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void About_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var text = String.Format(
                "プログラム名:\n    AskMonaViewer {0}\nホームページ:\n    https://github.com/tsurumeso/AskMonaViewer",
                mVersionString);
            MessageBox.Show(text, "AskMonaViewerについて", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            var viewTransactionsDialog = new ViewTransactionsDialog(this, mAskMonaApi, mSettings);
            viewTransactionsDialog.LoadSettings(mSettings.ViewTransactionsDialogSettings);
            viewTransactionsDialog.ShowDialog();
            mSettings = viewTransactionsDialog.Settings;
            mSettings.ViewTransactionsDialogSettings = viewTransactionsDialog.SaveSettings();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            var editProfileDialog = new EditProfileDialog(mAskMonaApi);
            editProfileDialog.LoadSettings(mSettings.EditProfileDialogSettings);
            editProfileDialog.ShowDialog();
            mSettings.EditProfileDialogSettings = editProfileDialog.SaveSettings();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var editTopicDialog = new EditTopicDialog(this, mAskMonaApi, mTopic);
            editTopicDialog.LoadSettings(mSettings.EditTopicDialogSettings);
            editTopicDialog.ShowDialog();
            mSettings.EditTopicDialogSettings = editTopicDialog.SaveSettings();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (mTopic == null || mScatterMonaDialog != null)
                return;

            var idx = mResponseCache.FindIndex(x => x.Topic.Id == mTopic.Id);
            if (idx == -1)
                return;

            mScatterMonaDialog = new ScatterMonaDialog(this, mSettings.Options, mAskMonaApi, mResponseCache[idx], mNGUsers);
            mScatterMonaDialog.LoadSettings(mSettings.ScatterMonaDialogSettings);
            mScatterMonaDialog.FormClosed += OnScatterMonaDialogClosed;
            mScatterMonaDialog.Show(this);
        }

        private async void listView1_Scroll(object sender, ScrollEventArgs e)
        {
            if (e.NewValue < 0)
                mWheelDelta += e.NewValue;

            if (listView1.Items.Count == 0 || mCategoryId == -1 || mIsTopicListUpdating || mWheelDelta > -120)
                return;

            mWheelDelta = 0;
            if (mTopIndex != 0 && mTopIndex == listView1.TopItem.Index)
            {
                mIsTopicListUpdating = true;
                UpdateConnectionStatus("通信中");
                if (await UpdateTopicList(mCategoryId, listView1.Items.Count))
                    UpdateConnectionStatus("受信完了");
                else
                    UpdateConnectionStatus("受信失敗");
                mIsTopicListUpdating = false;
            }
            mTopIndex = listView1.TopItem.Index;
        }

        private async void webBrowser_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (mTopic == null)
                return;

            if (e.KeyCode == Keys.F5)
            {
                e.IsInputKey = true;
                UpdateConnectionStatus("通信中");
                if (!(await UpdateResponse(mTopic.Id)))
                    UpdateConnectionStatus("受信失敗");
            }
        }

        private async void Option_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var setOptionDialog = new SetOptionDialog(mSettings.Options);
            setOptionDialog.ShowDialog();
            mSettings.Options = setOptionDialog.Options;
            await UpdateCurrencyPrice();
        }

        private async void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count <= 0 || mIsTabClosing)
                return;

            mTopic = (Topic)tabControl1.TabPages[tabControl1.SelectedIndex].Tag;
            toolStripComboBox1.Text = "https://askmona.org/" + mTopic.Id;

            if (mPostResponseDialog != null)
                mPostResponseDialog.UpdateTopic(mTopic);
            if (mScatterMonaDialog != null)
            {
                var idx = mResponseCache.FindIndex(x => x.Topic.Id == mTopic.Id);
                if (idx == -1)
                    return;
                mScatterMonaDialog.UpdateTopic(mResponseCache[idx]);
            }

            if (tabControl1.TabPages[tabControl1.SelectedIndex].Controls.Count > 0)
                mPrimaryWebBrowser = (WebBrowser)tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];
            else
            {
                UpdateConnectionStatus("通信中");
                if (!(await UpdateResponse(mTopic.Id)))
                    UpdateConnectionStatus("受信失敗");
            }

            UpdateFavoriteToolStrip();
        }

        private void CloseTab_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mIsTabClosing = true;

            var idx = tabControl1.SelectedIndex;
            if (tabControl1.TabPages[idx].Controls.Count > 0)
                tabControl1.TabPages[idx].Controls[0].Dispose();
            tabControl1.TabPages.RemoveAt(idx);

            mIsTabClosing = false;

            if (idx == 0) { }
            else if (idx == tabControl1.TabPages.Count)
                tabControl1.SelectedIndex = idx - 1;
            else
                tabControl1.SelectedIndex = idx;
        }

        private void CloseAllTab_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mIsTabClosing = true;

            for (int i = tabControl1.TabPages.Count - 1; i >= 0; i--)
            {
                if (tabControl1.TabPages[i].Controls.Count > 0)
                    tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }

            mIsTabClosing = false;
        }

        private void CloseTheOthers_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mIsTabClosing = true;

            for (int i = tabControl1.TabPages.Count - 1; i >= 0; i--)
            {
                var topic = (Topic)tabControl1.TabPages[i].Tag;
                if (topic.Id != mTopic.Id)
                {
                    if (tabControl1.TabPages[i].Controls.Count > 0)
                        tabControl1.TabPages[i].Controls[0].Dispose();
                    tabControl1.TabPages.RemoveAt(i);
                }
            }

            mIsTabClosing = false;
        }

        private void CloseLeft_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mIsTabClosing = true;

            for (int i = tabControl1.SelectedIndex - 1; i >= 0; i--)
            {
                if (tabControl1.TabPages[i].Controls.Count > 0)
                    tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }

            mIsTabClosing = false;
        }

        private void CloseRight_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            mIsTabClosing = true;

            for (int i = tabControl1.TabPages.Count - 1; i > tabControl1.SelectedIndex; i--)
            {
                if (tabControl1.TabPages[i].Controls.Count > 0)
                    tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }

            mIsTabClosing = false;
        }

        private void tabControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            for (int i = 0; i < tabControl1.TabCount; i++)
            {
                if (tabControl1.GetTabRect(i).Contains(e.X, e.Y))
                    tabControl1.SelectedIndex = i;
            }
        }

        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewItem lvi = listView1.GetItemAt(e.X, e.Y);

            if (lvi != mLastListViewItem)
            {
                var toolTip = new ToolTip();
                if (lvi != null)
                    toolTip.SetToolTip(listView1, lvi.ToolTipText);
                mLastListViewItem = lvi;
            }
        }

        private void toolStripButton16_Click(object sender, EventArgs e)
        {
            var viewImgurDialog = new ViewImgurDialog(mImgurApi, mImgurImageList);
            viewImgurDialog.LoadSettings(mSettings.ViewimgurDialogSettings);
            viewImgurDialog.ShowDialog();
            mSettings.ViewimgurDialogSettings = viewImgurDialog.SaveSettings();
            mImgurImageList = viewImgurDialog.ImgurImageList;
        }

        private void toolStripButton17_Click(object sender, EventArgs e)
        {
            var viewNGUsersDialog = new ViewNGUsersDialog(this, mAskMonaApi);
            viewNGUsersDialog.LoadSettings(mSettings.ViewNGUsersDialogSettings);
            viewNGUsersDialog.ShowDialog();
            mSettings.ViewNGUsersDialogSettings = viewNGUsersDialog.SaveSettings();
        }
    }
}
