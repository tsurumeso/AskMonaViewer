using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Threading.Tasks;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.Dialogs;
using AskMonaViewer.Settings;

namespace AskMonaViewer
{
    partial class MainForm
    {
        private int mCategoryId = 0;
        private int mTopIndex = 0;
        private int mWheelDelta = 0;
        private bool mIsTabClosing = false;
        private bool mIsDocumentLoading = false;
        private bool mIsTopicListUpdating = false;
        private string mHtmlHeader = "";
        private const string mVersionString = "2.0.3";
        private ApplicationSettings mSettings;
        private AskMonaApi mAskMonaApi;
        private ImgurApi mImgurApi;
        private ZaifApi mZaifApi;
        private Topic mTopic;
        private List<int> mNGUsers;
        private List<Topic> mTopicList;
        private List<Topic> mFavoriteTopicList;
        private List<ResponseList> mResponseCache;
        private List<ImgurImage> mImgurImageList;
        private PostResponseDialog mPostResponseDialog = null;
        private ScatterMonaDialog mScatterMonaDialog = null;
        private WebBrowser mPrimaryWebBrowser;
        private ListViewItem mLastListViewItem = null;
        private ListViewItemComparer mListViewItemSorter;

        private ListViewItem InitializeListViewItem(Topic topic, long time)
        {
            int newArrivals = topic.CachedCount == 0 ? 0 : topic.Count - topic.CachedCount;

            var lvi = new ListViewItem(
                new string[] {
                    topic.Rank.ToString(),
                    topic.Category,
                    topic.Title,
                    topic.Count.ToString(),
                    topic.CachedCount == 0 ? "" : topic.CachedCount.ToString(),
                    newArrivals == 0 ? "" : newArrivals.ToString(),
                    topic.Increased == 0 ? "" : topic.Increased.ToString(),
                    (Double.Parse(topic.Receive) / 100000000).ToString("F1"),
                    topic.Favorites.ToString(),
                    ((topic.Count / (double)(time - topic.Created)) * 3600 * 24).ToString("F1"),
                    Common.UnixTimeStampToDateTime(topic.Updated).ToString()
                }
            );
            lvi.Tag = topic;
            lvi.ToolTipText = topic.Lead;

            return lvi;
        }

        private async Task<bool> UpdateTopicList(int cat_id, int offset = 0)
        {
            TopicList topicList;
            if (cat_id == -1)
                topicList = await mAskMonaApi.FetchFavoriteTopicListAsync();
            else
                topicList = await mAskMonaApi.FetchTopicListAsync(cat_id, 50, offset);

            if (topicList == null)
                return false;

            listView1.BeginUpdate();
            var time = Common.DateTimeToUnixTimeStamp(DateTime.Now);
            foreach (var topic in topicList.Topics)
            {
                var oldTopic = mTopicList.Find(x => x.Id == topic.Id);
                if (oldTopic == null)
                    topic.Increased = 0;
                else
                    topic.Increased = topic.Count - oldTopic.Count;

                topic.CachedCount = 0;
                var cache = mResponseCache.Find(x => x.Topic.Id == topic.Id);
                if (cache != null)
                    topic.CachedCount = cache.Topic.Count;

                var lvi = InitializeListViewItem(topic, time);
                listView1.Items.Add(lvi);
            }
            listView1.ListViewItemSorter = mListViewItemSorter;
            Common.UpdateColumnColors(listView1, Color.White, Color.Lavender);
            listView1.EndUpdate();

            mTopicList.AddRange(topicList.Topics);

            return true;
        }

        private void UpdateTopicList(List<Topic> topicList)
        {
            listView1.Items.Clear();
            listView1.BeginUpdate();
            var time = Common.DateTimeToUnixTimeStamp(DateTime.Now);
            foreach (var topic in topicList)
            {
                var lvi = InitializeListViewItem(topic, time);
                listView1.Items.Add(lvi);
            }
            listView1.ListViewItemSorter = mListViewItemSorter;
            Common.UpdateColumnColors(listView1, Color.White, Color.Lavender);
            listView1.EndUpdate();
        }

        private bool FilterTopics(string key)
        {
            if (mTopicList.Count == 0)
                return false;

            if (String.IsNullOrEmpty(key))
            {
                UpdateTopicList(mTopicList);
                return false;
            }

            listView1.Items.Clear();
            listView1.BeginUpdate();
            var time = Common.DateTimeToUnixTimeStamp(DateTime.Now);
            foreach (var topic in mTopicList)
            {
                if (topic.Title.ToLower().Contains(key.ToLower()))
                {
                    var lvi = InitializeListViewItem(topic, time);
                    listView1.Items.Add(lvi);
                }
            }
            listView1.ListViewItemSorter = mListViewItemSorter;
            Common.UpdateColumnColors(listView1, Color.White, Color.Lavender);
            listView1.EndUpdate();

            return true;
        }

        private static string GetIdColorString(string userTimes)
        {
            int times = int.Parse(userTimes.Substring(userTimes.IndexOf("/") + 1));
            if (times == 1)
                return "black";
            else if (times < 5)
                return "green";
            else if (times < 10)
                return "blue";
            else
                return "red";
        }

        private static string ConvertResponse(Response response, int topicId)
        {
            var res = System.Security.SecurityElement.Escape(response.Text);
            res = Regex.Replace(res,
                @"https?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+",
                "<a href=\"$&\">$&</a>");
            res = Regex.Replace(res,
                @"(<a href=.+?>https?://)?(?<Imgur>(i.)?imgur.com/[a-zA-Z0-9]+)\.(?<Ext>[a-zA-Z]+)(</a>)?",
                "<a class=\"thumbnail\" href=\"https://${Imgur}.${Ext}\"><img src=\"https://${Imgur}m.${Ext}\"></a>");
            res = Regex.Replace(res,
                @"<a href=.+?>https?://(youtu.be/|(www.|m.)youtube.com/watch\?v=)(?<Id>[a-zA-Z0-9\-_]+)([\?\&].+)?</a>",
                "<a class=\"youtube\" name=\"${Id}\" href=\"javascript:void(0);\">" +
                "<img src=\"http://img.youtube.com/vi/${Id}/mqdefault.jpg\" width=\"480\" height=\"270\">サムネイルをクリックして動画を見る</a>");
            res = Regex.Replace(res,
                "&gt;&gt;(?<Id>[0-9]+)",
                String.Format("<a class=\"popup\" href=\"#res_{0}", topicId) + "_${Id}\">&gt;&gt;${Id}</a>");
            return res.Replace("\n", "<br>");
        }

        private async Task<string> BuildHtml(ResponseList responseList, bool showSupplyment = true)
        {
            StringBuilder html = new StringBuilder();

            await Task.Run(() =>
            {
                if (showSupplyment && !String.IsNullOrEmpty(mTopic.Supplyment))
                {
                    var response = new Response();
                    response.Text = mTopic.Supplyment;
                    html.Append("<p class=\"subtxt\">" + ConvertResponse(response, responseList.Topic.Id) + "</p>\n");
                }

                foreach (var response in responseList.Responses)
                {
                    if (showSupplyment && mNGUsers.Contains(response.UserId))
                        continue;

                    html.Append(String.Format(
                        "<a href=\"#res?r_id={0}\">{0}</a> 名前：<a href=\"#user?u_id={1}\" class=\"user\">{2}</a>{3} ",
                        response.Id, response.UserId, System.Security.SecurityElement.Escape(response.UserName + response.UserDan),
                        String.IsNullOrEmpty(response.UserTitle) ? "" : "・" + response.UserTitle));

                    if (mTopic.ShowHost > 0)
                        html.Append(String.Format("({0}) ", response.Host));

                    html.Append(String.Format(
                        "投稿日：{0} <font color={1}>ID：</font>{2} [{3}] <b>+{4}MONA/{5}人</b> <a href=\"#send?r_id={6}\" class=\"send\">←送る</a>\n",
                        Common.UnixTimeStampToDateTime(response.Created).ToString(), GetIdColorString(response.UserTimes), response.UserId,
                        response.UserTimes, Common.Digits(Double.Parse(response.Receive) / 100000000), response.ReceivedCount, response.Id));

                    if (response.Level < 2)
                        html.Append(String.Format("<p class=\"res_lv1\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 4)
                        html.Append(String.Format("<p class=\"res_lv2\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 5)
                        html.Append(String.Format("<p class=\"res_lv3\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 7)
                        html.Append(String.Format("<p class=\"res_lv4\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else
                        html.Append(String.Format("<p class=\"res_lv5\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                }
            });

            return html.ToString();
        }

        public async Task<string> BuildWebBrowserDocument(ResponseList responseList)
        {
            var html = await BuildHtml(responseList, false);
            return mHtmlHeader + html + "</body>\n</html>";
        }

        private void UpdateFavoriteToolStrip()
        {
            if (mFavoriteTopicList.Any(x => x.Id == mTopic.Id))
            {
                toolStripButton9.Image = imageList1.Images[1];
                toolStripButton9.Text = "お気に入り登録解除";
            }
            else
            {
                toolStripButton9.Image = imageList1.Images[2];
                toolStripButton9.Text = "お気に入り登録";
            }
        }

        private async Task<bool> MatchingLink(string link)
        {
            var matchRes = Regex.Match(link, @"about:blank#res\?r_id=(?<Id>[0-9]+)");
            var matchSend = Regex.Match(link, @"about:blank#send\?r_id=(?<Id>[0-9]+)");
            var matchUser = Regex.Match(link, @"about:blank#user\?u_id=(?<Id>[0-9]+)|https?://askmona.org/user/(?<Id>[0-9]+)");
            var matchAnchor = Regex.Match(link, @"about:blank#res_.+");
            var matchAskMona = Regex.Match(link, @"https?://askmona.org/(?<Id>[0-9]+)");
            if (matchRes.Success)
            {
                if (mPostResponseDialog != null)
                    mPostResponseDialog.UpdateTopic(mTopic, matchRes.Groups["Id"].Value);
                else
                {
                    mPostResponseDialog = new PostResponseDialog(this, mSettings.Options, mAskMonaApi, mImgurApi, mTopic, matchRes.Groups["Id"].Value);
                    mPostResponseDialog.LoadSettings(mSettings.PostResponseDialogSettings);
                    mPostResponseDialog.FormClosed += OnPostResponseDialogClosed;
                    mPostResponseDialog.Show(this);
                }
            }
            else if (matchSend.Success)
            {
                var sendMonaDialog = new SendMonaDialog(this, mSettings.Options, mAskMonaApi, mTopic, int.Parse(matchSend.Groups["Id"].Value));
                sendMonaDialog.LoadSettings(mSettings.SendMonaDialogSettings);
                sendMonaDialog.ShowDialog();
                mSettings.SendMonaDialogSettings = sendMonaDialog.SaveSettings();
            }
            else if (matchAskMona.Success)
            {
                var topicId = int.Parse(matchAskMona.Groups["Id"].Value);
                UpdateConnectionStatus("通信中");
                toolStripComboBox1.Text = "https://askmona.org/" + topicId;
                if (!(await UpdateResponse(topicId)))
                    UpdateConnectionStatus("受信失敗");
            }
            else if (matchUser.Success)
            {
                var viewProfileDialog = new ViewProfileDialog(this, mSettings.Options, mAskMonaApi, int.Parse(matchUser.Groups["Id"].Value));
                viewProfileDialog.LoadSettings(mSettings.ViewProfileDialogSettings);
                viewProfileDialog.ShowDialog();
                mSettings.ViewProfileDialogSettings = viewProfileDialog.SaveSettings();
            }
            else if (matchAnchor.Success)
                return false;
            else
                System.Diagnostics.Process.Start(link);

            return true;
        }

        private WebBrowser InitializeWebBrowser(string html)
        {
            var webBrowser = new WebBrowser();
            webBrowser.Dock = DockStyle.Fill;
            webBrowser.PreviewKeyDown += new PreviewKeyDownEventHandler(webBrowser_PreviewKeyDown);
            webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            webBrowser.IsWebBrowserContextMenuEnabled = false;
            webBrowser.ContextMenuStrip = contextMenuStrip1;
            webBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";
            return webBrowser;
        }

        private void AddTabPage(Topic topic, WebBrowser webBrowser = null)
        {
            var tabPage = new TabPage();
            tabPage.Padding = new Padding(3, 3, 3, 3);
            tabPage.BorderStyle = BorderStyle.FixedSingle;
            tabPage.UseVisualStyleBackColor = true;
            tabPage.Tag = topic;
            tabPage.ToolTipText = topic.Title;
            if (webBrowser != null)
                tabPage.Controls.Add(webBrowser);

            try
            {
                tabPage.Text = topic.Title.Substring(0, 15) + "...";
            }
            catch
            {
                tabPage.Text = topic.Title;
            }

            tabControl1.TabPages.Add(tabPage);
        }

        private void AddTabPage(Topic topic, string html)
        {
            WebBrowser webBrowser;
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (((Topic)tabControl1.TabPages[i].Tag).Id == topic.Id)
                {
                    if (tabControl1.TabPages[i].Controls.Count > 0)
                    {
                        mPrimaryWebBrowser = (WebBrowser)tabControl1.TabPages[i].Controls[0];
                        mPrimaryWebBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";
                    }
                    else
                    {
                        webBrowser = InitializeWebBrowser(html);
                        tabControl1.TabPages[i].Controls.Add(webBrowser);
                        mPrimaryWebBrowser = webBrowser;
                    }
                    tabControl1.TabPages[i].Tag = topic;
                    tabControl1.SelectedIndex = i;
                    return;
                }
            }

            webBrowser = InitializeWebBrowser(html);
            mPrimaryWebBrowser = webBrowser;
            AddTabPage(topic, webBrowser);
            tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
        }

        private async Task<bool> InitializeTabPage(int topicId)
        {
            Topic topic;
            var idx = mResponseCache.FindIndex(x => x.Topic.Id == topicId);
            if (idx == -1)
            {
                var responseList = await mAskMonaApi.FetchResponseListAsync(topicId, 1, 1, 1);
                if (responseList == null)
                    return false;
                topic = responseList.Topic;
            }
            else
            {
                var cache = mResponseCache[idx];
                topic = cache.Topic;
            }

            AddTabPage(topic);

            return true;
        }

        private async Task<bool> UpdateResponse(int topicId)
        {
            var html = "";
            var idx = mResponseCache.FindIndex(x => x.Topic.Id == topicId);
            if (idx == -1)
            {
                var responseList = await mAskMonaApi.FetchResponseListAsync(topicId, topic_detail: 1);
                if (responseList == null)
                    return false;
                mTopic = responseList.Topic;
                html = await BuildHtml(responseList);
                mResponseCache.Add(responseList);
            }
            else
            {
                var cache = mResponseCache[idx];
                var responseList = await mAskMonaApi.FetchResponseListAsync(topicId, 1, 1000, 1, cache.Topic.Modified);
                if (responseList == null)
                    return false;
                else if (responseList.Status == 2)
                {
                    mTopic = cache.Topic;
                    html = await BuildHtml(cache);
                }
                else
                {
                    mTopic = responseList.Topic;
                    mTopic.Scrolled = cache.Topic.Scrolled;
                    html = await BuildHtml(responseList);
                    mResponseCache.RemoveAt(idx);
                    mResponseCache.Add(responseList);
                }
            }

            mIsDocumentLoading = true;
            AddTabPage(mTopic, html);

            return true;
        }

        public async Task<bool> ReloadResponse()
        {
            var html = "";
            var responseList = await mAskMonaApi.FetchResponseListAsync(mTopic.Id, 1, 1000, 1);
            if (responseList == null)
                return false;

            var idx = mResponseCache.FindIndex(x => x.Topic.Id == mTopic.Id);
            var scrolled = 0;
            if (idx != -1)
            {
                scrolled = mResponseCache[idx].Topic.Scrolled;
                mResponseCache.RemoveAt(idx);
            }

            mTopic = responseList.Topic;
            mTopic.Scrolled = scrolled;
            html = await BuildHtml(responseList);
            mResponseCache.Add(responseList);

            mIsDocumentLoading = true;
            mPrimaryWebBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";

            return true;
        }

        private void OnScrollEventHandler(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var idx = mResponseCache.FindIndex(x => x.Topic.Id == mTopic.Id);
            if (idx == -1)
                return;

            var doc3 = (mshtml.IHTMLDocument3)mPrimaryWebBrowser.Document.DomDocument;
            var elm = (mshtml.IHTMLElement2)doc3.documentElement;
            var scrolled = elm.scrollTop;
            if (mSettings.Options.IsAlreadyReadPosition)
            {
                if (mResponseCache[idx].Topic.Scrolled < scrolled)
                    mResponseCache[idx].Topic.Scrolled = scrolled;
            }
            else if (mSettings.Options.IsLastPosition)
                mResponseCache[idx].Topic.Scrolled = scrolled;
        }

        private void OnPostResponseDialogClosed(object sender, EventArgs e)
        {
            mSettings.PostResponseDialogSettings = mPostResponseDialog.SaveSettings();
            mPostResponseDialog = null;
        }

        private void OnScatterMonaDialogClosed(object sender, EventArgs e)
        {
            mSettings.ScatterMonaDialogSettings = mScatterMonaDialog.SaveSettings();
            mScatterMonaDialog = null;
        }

        public void AddNGUser(int userId)
        {
            mNGUsers.Add(userId);
        }

        public void DeleteNGUser(int userId)
        {
            mNGUsers.Remove(userId);
        }

        public void AddImgurImage(ImgurImage imgurImage)
        {
            mImgurImageList.Add(imgurImage);
        }

        public void UpdateConnectionStatus(string label)
        {
            toolStripStatusLabel1.Text = label;
        }

        private async Task<bool> UpdateCurrenciesRate()
        {
            if (mSettings.Options.VisibleMonaJpy)
            {
                var rate = await mZaifApi.FetchRate("mona_jpy");
                if (rate == null)
                    return false;
                toolStripStatusLabel2.Text = "MONA/JPY " + rate.Last.ToString("F1");
            }
            else
                toolStripStatusLabel2.Text = "";

            if (mSettings.Options.VisibleBtcJpy)
            {
                var rate = await mZaifApi.FetchRate("btc_jpy");
                if (rate == null)
                    return false;
                toolStripStatusLabel3.Text = "BTC/JPY " + rate.Last.ToString("F0");
            }
            else
                toolStripStatusLabel3.Text = "";

            return true;
        }

        private void EnableControls()
        {
            timer1.Enabled = true;
            listView2.Enabled = true;
            contextMenuStrip2.Enabled = true;
            CloseTab_ToolStripMenuItem.Enabled = true;
            CloseAllTab_ToolStripMenuItem.Enabled = true;
            CloseTheOthers_ToolStripMenuItem.Enabled = true;
            CloseLeft_ToolStripMenuItem.Enabled = true;
            CloseRight_ToolStripMenuItem.Enabled = true;
        }

        private void LoadHtmlHeader()
        {
            mHtmlHeader = "<html lang=\"ja\">\n<head>\n" +
                "<meta charset=\"UTF-8\">\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n";

            if (File.Exists("common/style.css"))
            {
                var css = new StreamReader("common/style.css", Encoding.GetEncoding("UTF-8")).ReadToEnd();
                mHtmlHeader += String.Format("<style type=\"text/css\">\n{0}\n</style>\n", css);
            }

            if (File.Exists("common/script.js"))
            {
                var js = new StreamReader("common/script.js", Encoding.GetEncoding("UTF-8")).ReadToEnd();
                mHtmlHeader += String.Format(
                    "<script type=\"text/javascript\" src=\"https://code.jquery.com/jquery-2.2.4.min.js\"></script>\n" +
                    "<script type=\"text/javascript\">\n{0}\n</script>\n", js);
            }

            mHtmlHeader += "</head>\n<body>\n";
        }

        private void LoadSettings()
        {
            if (File.Exists("AskMonaViewer.xml"))
            {
                try
                {
                    var xs = new XmlSerializer(typeof(ApplicationSettings));
                    using (var sr = new StreamReader("AskMonaViewer.xml", new UTF8Encoding(false)))
                        mSettings = xs.Deserialize(sr) as ApplicationSettings;

                    this.WindowState = mSettings.MainFormSettings.WindowState;
                    this.Bounds = new Rectangle(mSettings.MainFormSettings.Location, mSettings.MainFormSettings.Size);
                    if (mSettings.MainFormSettings.IsHorizontal)
                    {
                        toolStripButton4.Checked = false;
                        toolStripButton5.Checked = true;
                        splitContainer1.Orientation = Orientation.Vertical;
                    }
                    this.splitContainer1.SplitterDistance = mSettings.MainFormSettings.VSplitterDistance;
                    this.splitContainer2.SplitterDistance = mSettings.MainFormSettings.HSplitterDistance;
                    mCategoryId = mSettings.MainFormSettings.CategoryId;
                }
                catch { }
            }

            if (File.Exists("ResponseCache.bin"))
            {
                var serializer = new DataContractSerializer(typeof(List<ResponseList>));
                using (var fs = new FileStream("ResponseCache.bin", FileMode.Open, FileAccess.Read))
                using (var binaryReader = XmlDictionaryReader.CreateBinaryReader(fs, new XmlDictionaryReaderQuotas()))
                    mResponseCache = serializer.ReadObject(binaryReader) as List<ResponseList>;
            }

            if (File.Exists("ImgurImageList.xml"))
            {
                var xs = new XmlSerializer(typeof(List<ImgurImage>));
                using (var sr = new StreamReader("ImgurImageList.xml", new UTF8Encoding(false)))
                    mImgurImageList = xs.Deserialize(sr) as List<ImgurImage>;
            }
        }

        private void SaveSettings()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                mSettings.MainFormSettings.Size = this.Bounds.Size;
                mSettings.MainFormSettings.Location = this.Bounds.Location;
            }
            else
            {
                mSettings.MainFormSettings.Size = this.RestoreBounds.Size;
                mSettings.MainFormSettings.Location = this.RestoreBounds.Location;
            }
            mSettings.MainFormSettings.WindowState = this.WindowState;
            mSettings.MainFormSettings.IsHorizontal = toolStripButton5.Checked;
            mSettings.MainFormSettings.VSplitterDistance = this.splitContainer1.SplitterDistance;
            mSettings.MainFormSettings.HSplitterDistance = this.splitContainer2.SplitterDistance;
            mSettings.MainFormSettings.CategoryId = mCategoryId;
            mSettings.MainFormSettings.SelectedTabIndex = tabControl1.SelectedIndex;
            mSettings.MainFormSettings.TabTopicList.Clear();

            foreach (TabPage tabPage in tabControl1.TabPages)
            {
                var topic = tabPage.Tag as Topic;
                mSettings.MainFormSettings.TabTopicList.Add(topic.Id);
            }

            var xs = new XmlSerializer(typeof(ApplicationSettings));
            using (var sw = new StreamWriter("AskMonaViewer.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mSettings);

            var serializer = new DataContractSerializer(typeof(List<ResponseList>));
            using (var fs = new FileStream("ResponseCache.bin", FileMode.Create, FileAccess.Write))
            using (var binaeyWriter = XmlDictionaryWriter.CreateBinaryWriter(fs))
                serializer.WriteObject(binaeyWriter, mResponseCache);

            xs = new XmlSerializer(typeof(List<ImgurImage>));
            using (var sw = new StreamWriter("ImgurImageList.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mImgurImageList);
        }
    }
}
