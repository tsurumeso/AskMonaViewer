using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.IO.Compression;
using System.Net.Http;
using System.Security;

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
        private const string mVersionString = "1.5.2";
        private Account mAccount;
        private AskMonaApi mApi;
        private ZaifApi mZaifApi;
        private HttpClient mHttpClient;
        private TopicComparer mListViewItemSorter;
        private Topic mTopic;
        private DateTime mUnixEpoch;
        private List<Topic> mTopicList;
        private List<Topic> mFavoriteTopicList;
        private List<ResponseCache> mResponseCacheList;
        private ResponseForm mResponseForm = null;

        public MainForm()
        {
            InitializeComponent();
            mListViewItemSorter = new TopicComparer();
            mListViewItemSorter.ColumnModes = new TopicComparer.ComparerMode[]
            {
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Double,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Double,
                TopicComparer.ComparerMode.DateTime
            };
            mTopicList = new List<Topic>();
            mFavoriteTopicList = new List<Topic>();
            mResponseCacheList = new List<ResponseCache>();
            mUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            mAccount = new Account();
            mHtmlHeader = "<html lang=\"ja\">\n<head>\n" +
                "<meta charset=\"UTF-8\">\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n";
        }

        public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            return mUnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private static string Digits(double value)
        {
            string valueString = value.ToString("F8").TrimEnd('0');

            int index = valueString.IndexOf('.');
            if (index == -1)
                return "F0";

            return String.Format("F{0}", valueString.Substring(index + 1).Length);
        }

        private void UpdateColumnColors()
        {
            bool flag = true;
            foreach (ListViewItem lvi in listView1.Items)
            {
                if (flag)
                    lvi.BackColor = System.Drawing.Color.White;
                else
                    lvi.BackColor = System.Drawing.Color.Lavender;
                flag = !flag;
            }
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
                    UnixTimeStampToDateTime(topic.Updated).ToString()
                }
            );
            return lvi;
        }

        private async Task<bool> UpdateTopicList(int cat_id, bool reflesh = true)
        {
            toolStripStatusLabel1.Text = "通信中";
            int offset = 0;
            if (reflesh)
            {
                listView1.Items.Clear();
                mTopicList.Clear();
            }
            else
                offset = listView1.Items.Count;

            TopicList topicList;
            if (cat_id == -1)
                topicList = await mApi.FetchFavoriteTopicListAsync();
            else
                topicList = await mApi.FetchTopicListAsync(cat_id, 50, offset);

            if (topicList == null)
            {
                toolStripStatusLabel1.Text = "受信失敗";
                return false;
            }

            listView1.BeginUpdate();
            var time = (long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds;
            foreach (var topic in topicList.Topics)
            {
                var oldTopic = mTopicList.Find(x => x.Id == topic.Id);
                if (oldTopic == null)
                    topic.Increased = 0;
                else
                    topic.Increased = topic.Count - oldTopic.Count;

                var cachedTopic = mResponseCacheList.Find(x => x.Topic.Id == topic.Id);
                topic.CachedCount = 0;
                if (cachedTopic != null)
                    topic.CachedCount = cachedTopic.Topic.Count;

                var lvi = CreateListViewItem(topic, time);
                lvi.Tag = topic;
                listView1.Items.Add(lvi);
            }
            mTopicList.AddRange(topicList.Topics);
            listView1.ListViewItemSorter = mListViewItemSorter;
            UpdateColumnColors();
            listView1.EndUpdate();
            toolStripStatusLabel1.Text = "受信完了";
            return true;
        }

        private void UpdateTopicList(List<Topic> topicList)
        {
            listView1.Items.Clear();
            listView1.BeginUpdate();
            var time = (long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds;
            foreach (var topic in topicList)
            {
                var lvi = CreateListViewItem(topic, time);
                lvi.Tag = topic;
                listView1.Items.Add(lvi);
            }
            listView1.ListViewItemSorter = mListViewItemSorter;
            UpdateColumnColors();
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
            var time = (long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds;
            foreach (var topic in mTopicList)
            {
                if (topic.Title.ToLower().Contains(key.ToLower()))
                {
                    var lvi = CreateListViewItem(topic, time);
                    lvi.Tag = topic;
                    listView1.Items.Add(lvi);
                }
            }
            listView1.ListViewItemSorter = mListViewItemSorter;
            UpdateColumnColors();
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

        private async Task<string> BuildHtml(ResponseList responseList, bool showSubtxt = true)
        {
            StringBuilder html = new StringBuilder();
            await Task.Run(() =>
            {
                if (showSubtxt && !String.IsNullOrEmpty(mTopic.Supplyment))
                    html.Append(String.Format("<p class=\"subtxt\">{0}</p>", mTopic.Supplyment.Replace("\n", "<br>")));

                foreach (var response in responseList.Responses)
                {
                    double receive = Double.Parse(response.Receive) / 100000000;
                    html.Append(String.Format("    <a href=#id>{0}</a> 名前：<a href=\"#user?u_id={1}\" class=\"user\">{2}</a> " +
                        "投稿日：{3} <font color={4}>ID：</font>{5} [{6}] <b>+{7}MONA/{8}人</b> <a href=\"#send?r_id={9}\" class=\"send\">←送る</a>\n",
                        response.Id, response.UserId, SecurityElement.Escape(response.UserName + response.UserDan),
                        UnixTimeStampToDateTime(response.Created).ToString(), GetIdColorString(response.UserTimes), response.UserId, response.UserTimes,
                        receive.ToString(Digits(receive)), response.ReceivedCount, response.Id));

                    var res = SecurityElement.Escape(response.Text);
                    res = Regex.Replace(res,
                        @"h?ttps?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+",
                        "<a href=\"$&\">$&</a>");
                    res = Regex.Replace(res,
                        @"<a href=.+>(?<Imgur>https?://(i.)?imgur.com/[a-zA-Z0-9]+)\.(?<Ext>[a-zA-Z]+)</a>",
                        "<a class=\"thumbnail\" href=\"${Imgur}.${Ext}\"><img src=\"${Imgur}m.${Ext}\"></a>");
                    res = Regex.Replace(res,
                        @"<a href=.+>https?://(youtu.be/)?(www.youtube.com/watch\?v=)?(?<Id>[a-zA-Z0-9\-_]+)([\?\&].+)?</a>",
                        "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/${Id}\" frameorder=\"0\" allowfullscreen></iframe>");
                    res = Regex.Replace(res,
                        "&gt;&gt;(?<Id>[0-9]+)",
                        String.Format("<a class=\"popup\" href=\"#res_{0}", responseList.Topic.Id) + "_${Id}\">&gt;&gt;${Id}</a>");
                    res = res.Replace("\n", "<br>");

                    if (response.Level < 2)
                        html.Append(String.Format("    <p class=\"res_lv1\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.Level < 4)
                        html.Append(String.Format("    <p class=\"res_lv2\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.Level < 5)
                        html.Append(String.Format("    <p class=\"res_lv3\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.Level < 7)
                        html.Append(String.Format("    <p class=\"res_lv4\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else
                        html.Append(String.Format("    <p class=\"res_lv5\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                }
            });
            return html.ToString();
        }

        public async Task<string> BuildWebBrowserDocument(ResponseList responseList)
        {
            var html = await BuildHtml(responseList, false);
            return mHtmlHeader + html + "</body>\n</html>";
        }

        private static string CompressString(string source)
        {
            var bytes = Encoding.UTF8.GetBytes(source);
            using (var ms = new MemoryStream())
            {
                var compressedStream = new DeflateStream(ms, CompressionMode.Compress, true);
                compressedStream.Write(bytes, 0, bytes.Length);
                // MemoryStream を読む前に Close
                compressedStream.Close();
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        private static string DecompressString(string source)
        {
            var bytes = Convert.FromBase64String(source);
            using (var ms = new MemoryStream(bytes))
            using (var buf = new MemoryStream())
            using (var CompressedStream = new DeflateStream(ms, CompressionMode.Decompress))
            {
                while (true)
                {
                    int rb = CompressedStream.ReadByte();
                    if (rb == -1)
                        break;
                    buf.WriteByte((byte)rb);
                }
                return Encoding.UTF8.GetString(buf.ToArray());
            }
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

        private async Task<bool> UpdateResponce(int topicId)
        {
            toolStripStatusLabel1.Text = "通信中";
            toolStripComboBox1.Text = "https://askmona.org/" + topicId;

            var html = "";
            var idx = mResponseCacheList.FindIndex(x => x.Topic.Id == topicId);
            if (idx == -1)
            {
                var responseList = await mApi.FetchResponseListAsync(topicId, topic_detail: 1);
                if (responseList == null)
                {
                    toolStripStatusLabel1.Text = "受信失敗";
                    return false;
                }
                mTopic = responseList.Topic;
                html = await BuildHtml(responseList);
                mResponseCacheList.Add(new ResponseCache(mTopic, CompressString(html.ToString())));
            }
            else
            {
                var cache = mResponseCacheList[idx];
                var responseList = await mApi.FetchResponseListAsync(topicId, 1, 1000, 1, cache.Topic.Modified);
                if (responseList == null)
                {
                    toolStripStatusLabel1.Text = "受信失敗";
                    return false;
                }
                if (responseList.Status == 2)
                {
                    mTopic = cache.Topic;
                    html = DecompressString(cache.Html);
                }
                else
                {
                    mTopic = responseList.Topic;
                    html = await BuildHtml(responseList);
                    mResponseCacheList.RemoveAt(idx);
                    mResponseCacheList.Add(new ResponseCache(mTopic, CompressString(html.ToString())));
                }
            }

            tabControl1.TabPages[0].Text = mTopic.Title;
            webBrowser1.DocumentText = mHtmlHeader + html + "</body>\n</html>";
            UpdateFavoriteToolStrip();
            return true;
        }

        public async Task<bool> ReloadResponce(int topicId)
        {
            toolStripStatusLabel1.Text = "通信中";
            toolStripComboBox1.Text = "https://askmona.org/" + topicId;

            var html = "";
            var responseList = await mApi.FetchResponseListAsync(topicId, 1, 1000, 1);
            if (responseList == null)
            {
                toolStripStatusLabel1.Text = "受信失敗";
                return false;
            }

            var idx = mResponseCacheList.FindIndex(x => x.Topic.Id == topicId);
            if (idx != -1)
                mResponseCacheList.RemoveAt(idx);

            mTopic = responseList.Topic;
            html = await BuildHtml(responseList);
            mResponseCacheList.Add(new ResponseCache(mTopic, CompressString(html.ToString())));

            tabControl1.TabPages[0].Text = mTopic.Title;
            webBrowser1.DocumentText = mHtmlHeader + html + "</body>\n</html>";
            UpdateFavoriteToolStrip();
            return true;
        }

        private async void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var topic = (Topic)listView1.SelectedItems[0].Tag;
            await UpdateResponce(topic.Id);
            if (mResponseForm == null || mResponseForm.IsDisposed)
                mResponseForm = new ResponseForm(this, mApi, topic);
            else
                mResponseForm.UpdateTopic(topic);
            listView1.SelectedItems[0].SubItems[4].Text = mTopic.Count.ToString();
            listView1.SelectedItems[0].SubItems[5].Text = "";
        }

        void Document_Click(object sender, HtmlElementEventArgs e)
        {
            try
            {
                string link = null;
                HtmlElement clickedElement = webBrowser1.Document.GetElementFromPoint(e.MousePosition);

                if (clickedElement.TagName == "a" || clickedElement.TagName == "A")
                    link = clickedElement.GetAttribute("href");
                else if (clickedElement.Parent != null)
                {
                    if (clickedElement.Parent.TagName == "a" || clickedElement.Parent.TagName == "A")
                        link = clickedElement.Parent.GetAttribute("href");
                }

                if (String.IsNullOrEmpty(link))
                    return;

                var mSend = Regex.Match(link, @"about:blank#send\?r_id=(?<Id>[0-9]+)");
                var mUser = Regex.Match(link, @"about:blank#user\?u_id=(?<Id>[0-9]+)");
                var mAnchor = Regex.Match(link, @"about:blank#res_.+");
                var mAskMona = Regex.Match(link, @"https?://askmona.org/(?<Id>[0-9]+)");
                if (mSend.Success)
                {
                    var monaRequestForm = new MonaSendForm(this, mApi, mTopic, int.Parse(mSend.Groups["Id"].Value));
                    monaRequestForm.ShowDialog();
                }
                else if (mAskMona.Success)
                {
                    // COMException 回避
                    var result = UpdateResponce(int.Parse(mAskMona.Groups["Id"].Value));
                }
                else if (mUser.Success)
                {
                    var profileViewForm = new ProfileViewForm(mApi, int.Parse(mUser.Groups["Id"].Value));
                    profileViewForm.ShowDialog();
                }
                else if (mAnchor.Success) { }
                else if (link == "about:blank#id") { }
                else
                    System.Diagnostics.Process.Start(link);
                e.ReturnValue = false;
            }
            catch (NullReferenceException) { }
        }

        private async void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                mCategoryId = int.Parse(listView2.SelectedItems[0].Tag.ToString());
                await UpdateTopicList(mCategoryId);
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (mHasDocumentLoaded)
            {
                this.webBrowser1.Document.Click -= new HtmlElementEventHandler(Document_Click);
                this.webBrowser1.Document.ContextMenuShowing -= new HtmlElementEventHandler(Document_ContextMenuShowing);
                mHasDocumentLoaded = false;
            }
            this.webBrowser1.Document.Click += new HtmlElementEventHandler(Document_Click);
            this.webBrowser1.Document.ContextMenuShowing += new HtmlElementEventHandler(Document_ContextMenuShowing);
            this.webBrowser1.Document.Body.ScrollIntoView(false);
            mHasDocumentLoaded = true;
            toolStripStatusLabel1.Text = "受信完了";
        }

        private void Document_ContextMenuShowing(object sender, HtmlElementEventArgs e)
        {
            var doc = (mshtml.IHTMLDocument2)this.webBrowser1.Document.DomDocument;
            var range = (mshtml.IHTMLTxtRange)doc.selection.createRange();
            var enabled = !String.IsNullOrEmpty(range.text);
            Copy_ToolStripMenuItem.Enabled = enabled;
            Search_ToolStripMenuItem.Enabled = enabled;
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorter.Column = e.Column;
            listView1.Sort();
            UpdateColumnColors();
        }

        private void comboBox1_TextUpdate(object sender, EventArgs e)
        {
            FilterTopics(comboBox1.Text);
        }

        private async void toolStripButton3_Click(object sender, EventArgs e)
        {
            await UpdateTopicList(mCategoryId);
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
            var rate = await mZaifApi.FetchRate("mona_jpy");
            if (rate != null)
                toolStripStatusLabel2.Text = "MONA/JPY " + rate.Last.ToString("F1");
        }

        private void toolStripButton6_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            if (mResponseForm == null || mResponseForm.IsDisposed)
                mResponseForm = new ResponseForm(this, mApi, mTopic);
            mResponseForm.Show();
        }

        public void SetAccount(string addr, string pass)
        {
            mAccount = new Account(addr, pass);
        }

        public void SetAccount(string authCode)
        {
            mAccount = new Account().FromAuthCode(authCode);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            if (File.Exists("AskMonaViewer.xml"))
            {
                var xs = new XmlSerializer(typeof(Account));
                using (var sr = new StreamReader("AskMonaViewer.xml", new UTF8Encoding(false)))
                    mAccount = xs.Deserialize(sr) as Account;
                if (String.IsNullOrEmpty(mAccount.SecretKey))
                {
                    var signUpForm = new SignUpForm(this, mAccount);
                    signUpForm.ShowDialog();
                }
            }
            else
            {
                var signUpForm = new SignUpForm(this, mAccount);
                signUpForm.ShowDialog();
            }
            if (File.Exists("ResponseCache.xml"))
            {
                var xs = new XmlSerializer(typeof(List<ResponseCache>));
                using (var sr = new StreamReader("ResponseCache.xml", new UTF8Encoding(false)))
                    mResponseCacheList = xs.Deserialize(sr) as List<ResponseCache>;
            }
            if (File.Exists("common/style.css"))
            {
                var css = new StreamReader("common/style.css", Encoding.GetEncoding("UTF-8")).ReadToEnd();
                mHtmlHeader += String.Format("<style type=\"text/css\">\n{0}\n</style>\n", css);
            }
            if (File.Exists("common/script.js"))
            {
                var js = new StreamReader("common/script.js", Encoding.GetEncoding("UTF-8")).ReadToEnd();
                mHtmlHeader += String.Format("<script type=\"text/javascript\" " +
                    "src=\"https://code.jquery.com/jquery-2.2.4.min.js\"></script>\n" +
                    "<script type=\"text/javascript\">\n{0}\n</script>\n", js);
            }
            mHtmlHeader += "</head>\n<body>\n";

            mHttpClient = new HttpClient();
            mHttpClient.Timeout = TimeSpan.FromSeconds(10.0);
            mZaifApi = new ZaifApi(mHttpClient);
            mApi = new AskMonaApi(mHttpClient, mAccount);
            var topicList = await mApi.FetchFavoriteTopicListAsync();
            if (topicList != null)
                mFavoriteTopicList = topicList.Topics;
            var rate = await mZaifApi.FetchRate("mona_jpy");
            if (rate != null)
                toolStripStatusLabel2.Text = "MONA/JPY " + rate.Last.ToString("F1");
            await UpdateTopicList(0);
            timer1.Enabled = true;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var xs = new XmlSerializer(typeof(Account));
            using (var sw = new StreamWriter("AskMonaViewer.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mAccount);

            xs = new XmlSerializer(typeof(List<ResponseCache>));
            using (var sw = new StreamWriter("ResponseCache.xml", false, new UTF8Encoding(false)))
                xs.Serialize(sw, mResponseCacheList);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            var topicCreateForm = new TopicCreateForm(mApi);
            topicCreateForm.ShowDialog();
        }

        private void toolStripButton8_Click(object sender, EventArgs e)
        {
            var monaWithdrawForm = new MonaWithdrawForm(mApi);
            monaWithdrawForm.ShowDialog();
        }

        private async void toolStripButton7_Click(object sender, EventArgs e)
        {
            var deposit = await mApi.FetchDepositAddressAsync();
            Clipboard.SetText(deposit.Address);
        }

        private void Copy_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.ExecCommand("Copy", false, null);
        }

        private void SelectAll_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            webBrowser1.Document.ExecCommand("SelectAll", false, null);
        }

        private void Search_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var doc = (mshtml.IHTMLDocument2)this.webBrowser1.Document.DomDocument;
            var range = (mshtml.IHTMLTxtRange)doc.selection.createRange();
            var url = "https://www.google.co.jp/#q=" + System.Net.WebUtility.UrlEncode(range.text);
            System.Diagnostics.Process.Start(url);
        }

        private async void toolStripButton9_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            int idx = mFavoriteTopicList.FindIndex(x => x.Id == mTopic.Id);
            if (idx == -1)
            {
                await mApi.AddFavoriteTopicAsync(mTopic.Id);
                mFavoriteTopicList.Add(mTopic);
            }
            else
            {
                await mApi.DeleteFavoriteTopicAsync(mTopic.Id);
                mFavoriteTopicList.RemoveAt(idx);
            }
            await UpdateTopicList(mCategoryId);
            UpdateFavoriteToolStrip();
        }

        private async void toolStripButton10_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var res = MessageBox.Show("トピックのキャッシュを削除して再度読み込みます\nよろしいですか？", "確認",
                MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2);

            if (res == DialogResult.Yes)
                await ReloadResponce(mTopic.Id);
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
            var transactionViewFrom = new TransactionViewForm(this, mApi);
            transactionViewFrom.ShowDialog();
        }

        private void toolStripButton12_Click(object sender, EventArgs e)
        {
            var profileEditForm = new ProfileEditForm(mApi);
            profileEditForm.ShowDialog();
        }

        private void toolStripButton13_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var topicEditForm = new TopicEditForm(mApi, mTopic);
            topicEditForm.ShowDialog();
        }

        private void toolStripButton14_Click(object sender, EventArgs e)
        {
            if (mTopic == null)
                return;

            var sendTogetherForm = new SendTogetherForm(this, mApi, mTopic);
            sendTogetherForm.ShowDialog();
        }

        private async void listView1_Scroll(object sender, ScrollEventArgs e)
        {
            mWheelDelta += e.NewValue;
            if (listView1.Items.Count == 0 || mCategoryId == -1 || mIsTopicListUpdating || mWheelDelta > -120)
                return;

            mWheelDelta = 0;
            if (mTopIndex != 0 && mTopIndex == listView1.TopItem.Index)
            {
                mIsTopicListUpdating = true;
                await UpdateTopicList(mCategoryId, false);
                mIsTopicListUpdating = false;
            }
            mTopIndex = listView1.TopItem.Index;
        }
    }
}
