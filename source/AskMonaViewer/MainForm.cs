using System;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace AskMonaViewer
{
    public partial class MainForm : Form
    {
        private Account mAccount = null;
        private AskMonaApi mApi;
        private ZaifApi mZaifApi;
        private int mCategoryId = 0;
        private bool mHasDocumentLoaded = true;
        private TopicComparer mListViewItemSorter;
        private Topic mTopic;
        private TopicList mTopicList;
        private DateTime mUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        private string mHtmlHeader;
        private List<ResponseCache> mResponseCacheList;

        public MainForm()
        {
            InitializeComponent();
            mListViewItemSorter = new TopicComparer();
            mListViewItemSorter.ColumnModes = new TopicComparer.ComparerMode[]
            {
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.Double,
                TopicComparer.ComparerMode.Double,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.DateTime
            };
            mTopicList = new TopicList();
            mZaifApi = new ZaifApi();
            mResponseCacheList = new List<ResponseCache>();
            var css = new StreamReader("css/style.css", Encoding.GetEncoding("UTF-8")).ReadToEnd();
            mHtmlHeader = String.Format("<html lang=\"ja\">\n<head>\n" +
                "<meta charset=\"UTF-8\">\n" +
                "<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\">\n" +
                "<style type=\"text/css\">\n{0}\n</style>" +
                "\n</head>\n" +
                "<body link=blue bgcolor=#E6E6E6>\n", css);
        }

        public DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            return mUnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private string WatanabeToMona(string wat)
        {
            return (Double.Parse(wat) / 100000000).ToString("F1");
        }

        private void RefreshColumnColors()
        {
            bool flag = true;
            foreach(ListViewItem lvi in listView1.Items)
            {
                if (flag)
                    lvi.BackColor = System.Drawing.Color.White;
                else
                    lvi.BackColor = System.Drawing.Color.Lavender;
                flag = !flag;
            }
        }

        private async void UpdateTopicList(int cat_id)
        {
            toolStripStatusLabel1.Text = "通信中";
            var topicList = await mApi.FetchTopicListAsync(cat_id, 250);

            if (topicList == null)
            {
                toolStripStatusLabel1.Text = "受信失敗";
                return;
            }

            listView1.Items.Clear();
            listView1.BeginUpdate();
            var time = (long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds;
            await Task.Run(() =>
            {
                foreach (var topic in topicList.Topics)
                {
                    var sameTopic = mTopicList.Topics.Find(x => x.Id == topic.Id);
                    if (sameTopic == null)
                        topic.Increased = 0;
                    else
                        topic.Increased = topic.Count - sameTopic.Count;

                    var lvi = new ListViewItem(
                        new string[] {
                        topic.Rank.ToString(),
                        topic.Category,
                        topic.Title,
                        WatanabeToMona(topic.ReceivedMona),
                        ((topic.Count / (double)(time - topic.Created)) * 3600 * 24).ToString("F1"),
                        topic.Increased.ToString(),
                        topic.Count.ToString(),
                        UnixTimeStampToDateTime(topic.Updated).ToString()
                        }
                    );
                    lvi.Tag = topic;
                    listView1.Items.Add(lvi);
                }
            });
            mTopicList = topicList;
            listView1.ListViewItemSorter = mListViewItemSorter;
            RefreshColumnColors();
            listView1.EndUpdate();
            toolStripStatusLabel1.Text = "受信完了";
        }

        private async void FilterTopics(string key)
        {
            if (mTopicList.Topics.Count == 0)
                return;

            if (String.IsNullOrEmpty(key))
            {
                UpdateTopicList(mCategoryId);
                return;
            }

            listView1.Items.Clear();
            listView1.BeginUpdate();
            var time = (long)(DateTime.Now.ToUniversalTime() - mUnixEpoch).TotalSeconds;
            await Task.Run(() =>
            {
                foreach (var topic in mTopicList.Topics)
                {
                    if (topic.Title.ToLower().Contains(key.ToLower()))
                    {
                        var lvi = new ListViewItem(
                            new string[] {
                            topic.Rank.ToString(),
                            topic.Category,
                            topic.Title,
                            WatanabeToMona(topic.ReceivedMona),
                            ((topic.Count / (double)(time - topic.Created)) * 3600 * 24).ToString("F1"),
                            topic.Increased.ToString(),
                            topic.Count.ToString(),
                            UnixTimeStampToDateTime(topic.Updated).ToString()
                            }
                        );
                        lvi.Tag = topic;
                        listView1.Items.Add(lvi);
                    }
                }
            });
            listView1.ListViewItemSorter = mListViewItemSorter;
            RefreshColumnColors();
            listView1.EndUpdate();
        }

        private async Task<string> BuildHtml(ResponseList responseList)
        {
            StringBuilder html = new StringBuilder();
            await Task.Run(() =>
            {
                foreach (var response in responseList.Responses)
                {
                    html.Append(String.Format("    <a href=#id>{0}</a> 名前：<font color=green><b>{1}さん</b></font> " +
                        "投稿日：{2} <font color=red>ID：</font>{3} [{4}] <b>+{5}MONA/{6}人</b> <a href=\"#send?r_id={7}\"><-送る</a>\n",
                        response.Id.ToString(), response.UserName + response.UserDan,
                        UnixTimeStampToDateTime(response.Created).ToString(), response.UserId, response.UserTimes,
                        WatanabeToMona(response.ReceivedMona), response.ReceivedCount, response.Id));

                    var res = Regex.Replace(response.Text,
                        @"<script.*>.*</script>",
                        "");
                    res = Regex.Replace(res,
                        @"h?ttps?://[-_.!~*'()a-zA-Z0-9;/?:@&=+$,%#]+",
                        "<a href=\"$&\">$&</a>");
                    res = Regex.Replace(res,
                        @"<a href=.+>(?<Imgur>https?://(i.)?imgur.com/[a-zA-Z0-9]+)\.(?<Ext>[a-zA-Z]+)</a>",
                        "<a class=\"thumbnail\" href=\"${Imgur}.${Ext}\"><img src=\"${Imgur}m.${Ext}\"></a>");
                    res = Regex.Replace(res,
                        @"<a href=.+>https?://(youtu.be/)?(www.youtube.com/watch\?v=)?(?<Id>[a-zA-Z0-9\-]+)([\?\&].+)?</a>",
                        "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/${Id}\" frameorder=\"0\" allowfullscreen></iframe>");
                    res = Regex.Replace(res,
                        ">>[0-9]+",
                        "<a class=\"tooltip\" href=\"#anchor\">$&<span class=\"tooltiptext\">anchor</span></a>");
                    res = res.Replace("\n", "<br>");

                    if (response.ReceivedLevel == 0)
                        html.Append(String.Format("    <p class=\"res_lv1\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.ReceivedLevel < 4)
                        html.Append(String.Format("    <p class=\"res_lv2\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.ReceivedLevel < 5)
                        html.Append(String.Format("    <p class=\"res_lv3\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else if (response.ReceivedLevel < 7)
                        html.Append(String.Format("    <p class=\"res_lv4\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                    else
                        html.Append(String.Format("    <p class=\"res_lv5\" style=\"padding-left: 32px;\">{0}</p>\n", res));
                }
            });
            return html.ToString();
        }

        private async void UpdateResponces(int topicId)
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
                    return;
                }
                mTopic = responseList.Topic;
                html = await BuildHtml(responseList);
                mResponseCacheList.Add(new ResponseCache(mTopic, html.ToString()));
            }
            else
            {
                var cache = mResponseCacheList[idx];
                var responseList = await mApi.FetchResponseListAsync(topicId, cache.Topic.Count + 1, 1000, 1, cache.Topic.Modified);
                if (responseList == null)
                {
                    toolStripStatusLabel1.Text = "受信失敗";
                    return;
                }
                if (responseList.Status == 2)
                {
                    var responseCache = cache;
                    mTopic = responseCache.Topic;
                    html = responseCache.Html;
                }
                else
                {
                    mTopic = responseList.Topic;
                    html = cache.Html + await BuildHtml(responseList);
                    mResponseCacheList.RemoveAt(idx);
                    mResponseCacheList.Add(new ResponseCache(mTopic, html.ToString()));
                }
            }
            tabControl1.TabPages[0].Text = mTopic.Title;
            webBrowser1.DocumentText = mHtmlHeader + html + "</body>\n</html>";
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var topic = (Topic)listView1.SelectedItems[0].Tag;
            UpdateResponces(topic.Id);
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

                if (!String.IsNullOrEmpty(link))
                {
                    var reSend = new Regex(@"about:blank#send\?r_id=(?<Id>[0-9]+)");
                    var reAskMona = new Regex(@"https?://askmona.org/(?<Id>[0-9]+)");
                    var mSend = reSend.Match(link);
                    var mAskMona = reAskMona.Match(link);
                    if (mSend.Success)
                    {
                        var monaRequestForm = new MonaSendForm(mApi, mTopic.Id, int.Parse(mSend.Groups["Id"].Value));
                        monaRequestForm.StartPosition = FormStartPosition.CenterScreen;
                        monaRequestForm.ShowDialog();
                    }
                    else if (mAskMona.Success)
                        UpdateResponces(int.Parse(mAskMona.Groups["Id"].Value));
                    else if (link == "about:blank#id")
                    {
                    }
                    else
                        System.Diagnostics.Process.Start(link);
                    e.ReturnValue = false;
                }
            }
            catch (NullReferenceException) { }
        }

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count > 0)
            {
                mCategoryId = int.Parse(listView2.SelectedItems[0].Tag.ToString());
                UpdateTopicList(mCategoryId);
            }
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (mHasDocumentLoaded)
            {
                this.webBrowser1.Document.Click -= new HtmlElementEventHandler(Document_Click);
                mHasDocumentLoaded = false;
            }
            this.webBrowser1.Document.Click += new HtmlElementEventHandler(Document_Click);
            this.webBrowser1.Document.Body.ScrollIntoView(false);
            mHasDocumentLoaded = true;
            toolStripStatusLabel1.Text = "受信完了";
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorter.Column = e.Column;
            listView1.Sort();
            RefreshColumnColors();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FilterTopics(comboBox1.Text);
        }

        private void comboBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            FilterTopics(comboBox1.Text);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            UpdateTopicList(mCategoryId);
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
            if (mTopic != null)
            {
                var responseForm = new ResponseForm(mApi, mTopic.Id);
                responseForm.StartPosition = FormStartPosition.CenterScreen;
                responseForm.ShowDialog();
            }
        }

        public void SetAccount(string addr, string pass)
        {
            mAccount = new Account(addr, pass);
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                var xs = new XmlSerializer(typeof(Account));
                using (var sr = new StreamReader("AskMonaViewer.xml", new UTF8Encoding(false)))
                {
                    mAccount = xs.Deserialize(sr) as Account;
                }
            }
            catch
            {
                var loginForm = new LoginForm(this);
                loginForm.StartPosition = FormStartPosition.CenterScreen;
                loginForm.ShowDialog();
                if (mAccount == null)
                    mAccount = new Account();
            }
            try
            {
                var xs = new XmlSerializer(typeof(List<ResponseCache>));
                using (var sr = new StreamReader("ResponseCache.xml", new UTF8Encoding(false)))
                {
                    mResponseCacheList = xs.Deserialize(sr) as List<ResponseCache>;
                }
            }
            catch { }
            mApi = new AskMonaApi(mAccount);
            var rate = await mZaifApi.FetchRate("mona_jpy");
            if (rate != null)
                toolStripStatusLabel2.Text = "MONA/JPY " + rate.Last.ToString("F1");
            UpdateTopicList(0);
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
            topicCreateForm.StartPosition = FormStartPosition.CenterScreen;
            topicCreateForm.ShowDialog();
        }
    }
}
