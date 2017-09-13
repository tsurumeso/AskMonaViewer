using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Net.Http;
using System.Xml.Serialization;
using System.Threading.Tasks;

using AskMonaWrapper;
using AskMonaViewer.Utilities;
using AskMonaViewer.SubForms;
using AskMonaViewer.Settings;

namespace AskMonaViewer
{
    public partial class MainForm : Form
    {
        private int mCategoryId = 0;
        private int mTopIndex = 0;
        private int mWheelDelta = 0;
        private bool mHasDocumentLoaded = true;
        private bool mIsTopicListUpdating = false;
        private string mHtmlHeader = "";
        private const string mVersionString = "1.7.3";
        private ApplicationSettings mSettings;
        private AskMonaApi mAskMonaApi;
        private ZaifApi mZaifApi;
        private HttpClient mHttpClient;
        private ListViewItemComparer mListViewItemSorter;
        private Topic mTopic;
        private List<Topic> mTopicList;
        private List<Topic> mFavoriteTopicList;
        private List<ResponseCache> mResponseCacheList;
        private ResponseForm mResponseForm = null;
        private WebBrowser mPrimaryWebBrowser;

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
            mTopicList = new List<Topic>();
            mFavoriteTopicList = new List<Topic>();
            mResponseCacheList = new List<ResponseCache>();
            mSettings = new ApplicationSettings();
            mHttpClient = new HttpClient();
            mHttpClient.Timeout = TimeSpan.FromSeconds(10.0);
            mZaifApi = new ZaifApi(mHttpClient);
        }

        private ListViewItem CreateListViewItem(Topic topic, long time)
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
                var cache = mResponseCacheList.Find(x => x.Topic.Id == topic.Id);
                if (cache != null)
                    topic.CachedCount = cache.Topic.Count;

                var lvi = CreateListViewItem(topic, time);
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
                var lvi = CreateListViewItem(topic, time);
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
                    var lvi = CreateListViewItem(topic, time);
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
                @"<a href=.+?>(?<Imgur>https?://(i.)?imgur.com/[a-zA-Z0-9]+)\.(?<Ext>[a-zA-Z]+)</a>",
                "<a class=\"thumbnail\" href=\"${Imgur}.${Ext}\"><img src=\"${Imgur}m.${Ext}\"></a>");
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
                    html.Append("<p class=\"subtxt\">" + ConvertResponse(response, responseList.Topic.Id) + "</p>");
                }

                foreach (var response in responseList.Responses)
                {
                    html.Append(String.Format("    <a href=\"javascript:void(0);\">{0}</a> 名前：<a href=\"#user?u_id={1}\" class=\"user\">{2}</a> " +
                        "投稿日：{3} <font color={4}>ID：</font>{5} [{6}] <b>+{7}MONA/{8}人</b> <a href=\"#send?r_id={9}\" class=\"send\">←送る</a>\n",
                        response.Id, response.UserId, System.Security.SecurityElement.Escape(response.UserName + response.UserDan),
                        Common.UnixTimeStampToDateTime(response.Created).ToString(), GetIdColorString(response.UserTimes), response.UserId,
                        response.UserTimes, Common.Digits(Double.Parse(response.Receive) / 100000000), response.ReceivedCount, response.Id));

                    if (response.Level < 2)
                        html.Append(String.Format("    <p class=\"res_lv1\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 4)
                        html.Append(String.Format("    <p class=\"res_lv2\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 5)
                        html.Append(String.Format("    <p class=\"res_lv3\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else if (response.Level < 7)
                        html.Append(String.Format("    <p class=\"res_lv4\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
                    else
                        html.Append(String.Format("    <p class=\"res_lv5\">{0}</p>\n", ConvertResponse(response, responseList.Topic.Id)));
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

        private void AddTabPage(string html, Topic topic)
        {
            for (int i = 0; i < tabControl1.TabPages.Count; i++)
            {
                if (((Topic)tabControl1.TabPages[i].Tag).Id == topic.Id)
                {
                    mPrimaryWebBrowser = (WebBrowser)tabControl1.TabPages[i].Controls[0];
                    mPrimaryWebBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";
                    tabControl1.SelectedIndex = i;
                    return;
                }
            }

            var webBrowser = new WebBrowser();
            webBrowser.Dock = DockStyle.Fill;
            webBrowser.PreviewKeyDown += new PreviewKeyDownEventHandler(webBrowser_PreviewKeyDown);
            webBrowser.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(webBrowser_DocumentCompleted);
            webBrowser.IsWebBrowserContextMenuEnabled = false;
            webBrowser.ContextMenuStrip = contextMenuStrip1;
            webBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";
            mPrimaryWebBrowser = webBrowser;

            var tabPage = new TabPage();
            tabPage.Padding = new Padding(3, 3, 3, 3);
            tabPage.BorderStyle = BorderStyle.FixedSingle;
            tabPage.UseVisualStyleBackColor = true;
            tabPage.Controls.Add(webBrowser);
            tabPage.Tag = topic;
            tabPage.ToolTipText = topic.Title;

            try
            {
                tabPage.Text = topic.Title.Substring(0, 15) + "...";
            }
            catch
            {
                tabPage.Text = topic.Title;
            }

            tabControl1.TabPages.Add(tabPage);
            tabControl1.SelectedIndex = tabControl1.TabPages.Count - 1;
        }

        private async Task<bool> UpdateResponse(int topicId)
        {
            var html = "";
            var idx = mResponseCacheList.FindIndex(x => x.Topic.Id == topicId);
            if (idx == -1)
            {
                var responseList = await mAskMonaApi.FetchResponseListAsync(topicId, topic_detail: 1);
                if (responseList == null)
                    return false;
                mTopic = responseList.Topic;
                html = await BuildHtml(responseList);
                mResponseCacheList.Add(new ResponseCache(mTopic, Common.CompressString(html.ToString())));
            }
            else
            {
                var cache = mResponseCacheList[idx];
                var responseList = await mAskMonaApi.FetchResponseListAsync(topicId, 1, 1000, 1, cache.Topic.Modified);
                if (responseList == null)
                    return false;
                else if (responseList.Status == 2)
                {
                    mTopic = cache.Topic;
                    html = Common.DecompressString(cache.Html);
                }
                else
                {
                    mTopic = responseList.Topic;
                    mTopic.Scrolled = cache.Topic.Scrolled;
                    html = await BuildHtml(responseList);
                    mResponseCacheList.RemoveAt(idx);
                    mResponseCacheList.Add(new ResponseCache(mTopic, Common.CompressString(html.ToString())));
                }
            }

            mHasDocumentLoaded = false;
            AddTabPage(html, mTopic);
            UpdateFavoriteToolStrip();

            return true;
        }

        public async Task<bool> ReloadResponse()
        {
            var html = "";
            var responseList = await mAskMonaApi.FetchResponseListAsync(mTopic.Id, 1, 1000, 1);
            if (responseList == null)
                return false;

            var idx = mResponseCacheList.FindIndex(x => x.Topic.Id == mTopic.Id);
            var scrolled = new Point(0, 0);
            if (idx != -1)
            {
                scrolled = mResponseCacheList[idx].Topic.Scrolled;
                mResponseCacheList.RemoveAt(idx);
            }

            mTopic = responseList.Topic;
            mTopic.Scrolled = scrolled;
            html = await BuildHtml(responseList);
            mResponseCacheList.Add(new ResponseCache(mTopic, Common.CompressString(html.ToString())));

            mHasDocumentLoaded = false;
            mPrimaryWebBrowser.DocumentText = mHtmlHeader + html + "</body>\n</html>";
            UpdateFavoriteToolStrip();

            return true;
        }

        private void OnScrollEventHandler(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var idx = mResponseCacheList.FindIndex(x => x.Topic.Id == mTopic.Id);
            if (idx == -1)
                return;

            var doc3 = (mshtml.IHTMLDocument3)mPrimaryWebBrowser.Document.DomDocument;
            var elm = (mshtml.IHTMLElement2)doc3.documentElement;
            var scrolled = new Point(elm.scrollLeft, elm.scrollTop);
            if (mResponseCacheList[idx].Topic.Scrolled.Y < scrolled.Y)
                mResponseCacheList[idx].Topic.Scrolled = scrolled;
        }

        private void OnResponseFormClosed(object sender, EventArgs e)
        {
            mSettings.ResponseFormSettings = mResponseForm.SaveSettings();
            mResponseForm = null;
        }

        public void SetAccount(string addr, string pass)
        {
            mSettings.Account = new Account(addr, pass);
        }

        public void SetAccount(string authCode)
        {
            mSettings.Account = new Account().FromAuthCode(authCode);
        }

        public void SetOption(Options options)
        {
            mSettings.Options = options;
        }

        public void UpdateConnectionStatus(string label)
        {
            toolStripStatusLabel1.Text = label;
        }

        private async Task<bool> UpdateCurrenciesRate()
        {
            var rate = await mZaifApi.FetchRate("mona_jpy");
            if (rate == null)
                return false;
            toolStripStatusLabel2.Text = "MONA/JPY " + rate.Last.ToString("F1");

            rate = await mZaifApi.FetchRate("btc_jpy");
            if (rate == null)
                return false;
            toolStripStatusLabel3.Text = "BTC/JPY " + rate.Last.ToString("F0");

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
                mHtmlHeader += String.Format("<script type=\"text/javascript\" src=\"https://code.jquery.com/jquery-2.2.4.min.js\"></script>\n" +
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

            if (File.Exists("ResponseCache.xml"))
            {
                var xs = new XmlSerializer(typeof(List<ResponseCache>));
                using (var sr = new StreamReader("ResponseCache.xml", new UTF8Encoding(false)))
                    mResponseCacheList = xs.Deserialize(sr) as List<ResponseCache>;
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

            xs = new XmlSerializer(typeof(List<ResponseCache>));
            using (var sw = new StreamWriter("ResponseCache.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mResponseCacheList);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            LoadSettings();
            LoadHtmlHeader();

            mAskMonaApi = new AskMonaApi(mHttpClient, "3738", "AgGu661B9pe9SL49soov7tZNYRzdF4n8TUjsqNUTOTu0=", mSettings.Account);
            if (await mAskMonaApi.VerifySecretKey() == null)
            {
                var signUpForm = new SignUpForm(this, mSettings.Account);
                signUpForm.ShowDialog();
                mAskMonaApi.Account = mSettings.Account;
            }

            var topicList = await mAskMonaApi.FetchFavoriteTopicListAsync();
            if (topicList != null)
                mFavoriteTopicList = topicList.Topics;

            foreach (var topicId in mSettings.MainFormSettings.TabTopicList)
            {
                UpdateConnectionStatus("通信中");
                toolStripComboBox1.Text = "https://askmona.org/" + topicId;
                if (!(await UpdateResponse(topicId)))
                    UpdateConnectionStatus("受信失敗");
                else
                    await Task.Run(() => { while (!mHasDocumentLoaded) System.Threading.Thread.Sleep(100); });
            }
            tabControl1.SelectedIndex = mSettings.MainFormSettings.SelectedTabIndex;

            UpdateConnectionStatus("通信中");
            if (await UpdateTopicList(mCategoryId))
                UpdateConnectionStatus("受信完了");
            else
                UpdateConnectionStatus("受信失敗");

            await UpdateCurrenciesRate();
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

            if (mResponseForm != null)
                mResponseForm.UpdateTopic(topic);

            var topicIndex = mTopicList.FindIndex(x => x.Id == topic.Id);
            if (topicIndex != -1)
                mTopicList[topicIndex].CachedCount = mTopic.Count;

            listView1.Items[itemIndex].SubItems[4].Text = mTopic.Count.ToString();
            listView1.Items[itemIndex].SubItems[5].Text = "";
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

            var matchSend = Regex.Match(link, @"about:blank#send\?r_id=(?<Id>[0-9]+)");
            var matchUser = Regex.Match(link, @"about:blank#user\?u_id=(?<Id>[0-9]+)");
            var matchAnchor = Regex.Match(link, @"about:blank#res_.+");
            var matchAskMona = Regex.Match(link, @"https?://askmona.org/(?<Id>[0-9]+)");
            if (matchSend.Success)
            {
                var monaSendForm = new MonaSendForm(this, mSettings.Options, mAskMonaApi, mTopic, int.Parse(matchSend.Groups["Id"].Value));
                monaSendForm.LoadSettings(mSettings.MonaSendFormSettings);
                monaSendForm.ShowDialog();
                mSettings.MonaSendFormSettings = monaSendForm.SaveSettings();
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
                var profileViewForm = new ProfileViewForm(mSettings.Options, mAskMonaApi, int.Parse(matchUser.Groups["Id"].Value));
                profileViewForm.LoadSettings(mSettings.ProfileViewFormSettings);
                profileViewForm.ShowDialog();
                mSettings.ProfileViewFormSettings = profileViewForm.SaveSettings();
            }
            else if (matchAnchor.Success) { }
            else
                System.Diagnostics.Process.Start(link);
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
            if (!mHasDocumentLoaded)
                mPrimaryWebBrowser.Document.Window.ScrollTo(mTopic.Scrolled);

            mHasDocumentLoaded = true;
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
            await UpdateCurrenciesRate();
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            if (mResponseForm == null)
            {
                mResponseForm = new ResponseForm(this, mSettings.Options, mAskMonaApi, mTopic);
                mResponseForm.LoadSettings(mSettings.ResponseFormSettings);
                mResponseForm.FormClosed += OnResponseFormClosed;
            }
            mResponseForm.Show();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var topicCreateForm = new TopicCreateForm(mAskMonaApi);
            topicCreateForm.LoadSettings(mSettings.TopicCreateFormSettings);
            topicCreateForm.ShowDialog();
            mSettings.TopicCreateFormSettings = topicCreateForm.SaveSettings();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            var monaWithdrawForm = new MonaWithdrawForm(mAskMonaApi);
            monaWithdrawForm.ShowDialog();
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
            var text = String.Format("プログラム名:\n    AskMonaViewer {0}\nホームページ:\n    https://github.com/tsurumeso/AskMonaViewer",
                mVersionString);
            MessageBox.Show(text, "AskMonaViewerについて", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void toolStripButton11_Click(object sender, EventArgs e)
        {
            var transactionViewFrom = new TransactionViewForm(this, mAskMonaApi);
            transactionViewFrom.LoadSettings(mSettings.TransactionViewFormSettings);
            transactionViewFrom.ShowDialog();
            mSettings.TransactionViewFormSettings = transactionViewFrom.SaveSettings();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            var profileEditForm = new ProfileEditForm(mAskMonaApi);
            profileEditForm.LoadSettings(mSettings.ProfileEditFormSettings);
            profileEditForm.ShowDialog();
            mSettings.ProfileEditFormSettings = profileEditForm.SaveSettings();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var topicEditForm = new TopicEditForm(this, mAskMonaApi, mTopic);
            topicEditForm.LoadSettings(mSettings.TopicEditFormSettings);
            topicEditForm.ShowDialog();
            mSettings.TopicEditFormSettings = topicEditForm.SaveSettings();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var monaScatterForm = new MonaScatterForm(this, mSettings.Options, mAskMonaApi, mTopic);
            monaScatterForm.LoadSettings(mSettings.MonaScatterFormSettings);
            monaScatterForm.ShowDialog();
            mSettings.MonaScatterFormSettings = monaScatterForm.SaveSettings();
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
            e.IsInputKey = true;
            if (mTopic == null)
                return;

            if (e.KeyCode == Keys.F5)
            {
                UpdateConnectionStatus("通信中");
                if (!(await UpdateResponse(mTopic.Id)))
                    UpdateConnectionStatus("受信失敗");
            }
        }

        private void Option_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var optionForm = new OptionForm(this, mSettings.Options);
            optionForm.ShowDialog();
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.TabPages.Count <= 0)
                return;

            mTopic = (Topic)tabControl1.TabPages[tabControl1.SelectedIndex].Tag;
            toolStripComboBox1.Text = "https://askmona.org/" + mTopic.Id;
            if (mResponseForm != null)
                mResponseForm.UpdateTopic(mTopic);
            mPrimaryWebBrowser = (WebBrowser)tabControl1.TabPages[tabControl1.SelectedIndex].Controls[0];
            UpdateFavoriteToolStrip();
        }

        private void CloseTab_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = tabControl1.SelectedIndex;
            tabControl1.TabPages[index].Controls[0].Dispose();
            tabControl1.TabPages.RemoveAt(index);

            if (index == 0) { }
            else if (index == tabControl1.TabPages.Count)
                tabControl1.SelectedIndex = index - 1;
            else
                tabControl1.SelectedIndex = index;
        }

        private void CloseAllTab_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = tabControl1.TabPages.Count - 1; i >= 0; i--)
            {
                tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }
        }

        private void CloseTheOthers_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = tabControl1.TabPages.Count - 1; i >= 0; i--)
            {
                var topic = (Topic)tabControl1.TabPages[i].Tag;
                if (topic.Id != mTopic.Id)
                {
                    tabControl1.TabPages[i].Controls[0].Dispose();
                    tabControl1.TabPages.RemoveAt(i);
                }
            }
        }

        private void CloseLeft_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = tabControl1.SelectedIndex - 1; i >= 0; i--)
            {
                tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }
        }

        private void CloseRight_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = tabControl1.TabPages.Count - 1; i > tabControl1.SelectedIndex; i--)
            {
                tabControl1.TabPages[i].Controls[0].Dispose();
                tabControl1.TabPages.RemoveAt(i);
            }
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
    }
}
