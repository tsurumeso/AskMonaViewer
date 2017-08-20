using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

namespace AskMonaViewer
{
    public partial class TransactionViewForm : Form
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private TopicComparer mListViewItemSorterDW;
        private TopicComparer mListViewItemSorterRS;

        public TransactionViewForm(MainForm parent, AskMonaApi api)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mListViewItemSorterDW = new TopicComparer();
            mListViewItemSorterDW.ColumnModes = new TopicComparer.ComparerMode[]
            {
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.DateTime,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.Double
            };
            mListViewItemSorterRS = new TopicComparer();
            mListViewItemSorterRS.ColumnModes = new TopicComparer.ComparerMode[]
            {
                TopicComparer.ComparerMode.Integer,
                TopicComparer.ComparerMode.DateTime,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.String,
                TopicComparer.ComparerMode.Double
            };
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private void UpdateColumnColors(ListViewEx listView)
        {
            bool flag = true;
            foreach (ListViewItem lvi in listView.Items)
            {
                if (flag)
                    lvi.BackColor = System.Drawing.Color.White;
                else
                    lvi.BackColor = System.Drawing.Color.Lavender;
                flag = !flag;
            }
        }

        private async void TransactionViewForm_Load(object sender, EventArgs e)
        {
            var deposit = await mApi.FetchTransactionAsync("deposit");
            var withdraw = await mApi.FetchTransactionAsync("withdraw");
            var receive = await mApi.FetchTransactionAsync("receive");
            var send = await mApi.FetchTransactionAsync("send");

            if (deposit != null && withdraw != null)
            {
                var txs = new List<Transaction>(deposit.Transactions);
                txs.AddRange(withdraw.Transactions);
                txs = txs.OrderBy(x => x.Created).ToList();
                listViewEx1.BeginUpdate();
                for (int i = 0; i < txs.Count; i++)
                {
                    var lvi = new ListViewItem(
                        new string[] {
                            (i + 1).ToString(),
                            UnixTimeStampToDateTime(txs[i].Created).ToString(),
                            txs[i].Item == "deposit" ? "入金" : "出金",
                            (Double.Parse(txs[i].Amount) / 100000000).ToString("F8")
                        }
                    );
                    listViewEx1.Items.Add(lvi);
                }
                listViewEx1.ListViewItemSorter = mListViewItemSorterDW;
                UpdateColumnColors(listViewEx1);
                listViewEx1.EndUpdate();
            }

            if (receive != null && send != null)
            {
                var txs = new List<Transaction>(receive.Transactions);
                txs.AddRange(send.Transactions);
                txs = txs.OrderBy(x => x.Created).ToList();
                listViewEx2.BeginUpdate();
                for (int i = 0; i < txs.Count; i++)
                {
                    var lvi = new ListViewItem(
                        new string[] {
                            (i + 1).ToString(),
                            UnixTimeStampToDateTime(txs[i].Created).ToString(),
                            txs[i].Item == "receive" ? "受け取り" : "ばらまき",
                            txs[i].User != null ? txs[i].User.UserName + txs[i].User.UserDan : "匿名",
                            (Double.Parse(txs[i].Amount) / 100000000).ToString("F8")
                        }
                    );
                    lvi.Tag = txs[i];
                    listViewEx2.Items.Add(lvi);
                }
                listViewEx2.ListViewItemSorter = mListViewItemSorterRS;
                UpdateColumnColors(listViewEx2);
                listViewEx2.EndUpdate();
            }
        }

        private void listViewEx1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorterDW.Column = e.Column;
            listViewEx1.Sort();
            UpdateColumnColors(listViewEx1);
            for (int i = 0; i < listViewEx1.Items.Count; i++)
                listViewEx1.Items[i].SubItems[0].Text = (i + 1).ToString();
        }

        private void listViewEx2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorterRS.Column = e.Column;
            listViewEx2.Sort();
            UpdateColumnColors(listViewEx2);
            for (int i = 0; i < listViewEx2.Items.Count; i++)
                listViewEx2.Items[i].SubItems[0].Text = (i + 1).ToString();
        }

        private async void listViewEx2_DoubleClick(object sender, EventArgs e)
        {
            var tx = (Transaction)listViewEx2.SelectedItems[0].Tag;
            var responseList = await mApi.FetchResponseListAsync(tx.TopicId, tx.ResponceId, tx.ResponceId, 1);
            var html = await mParent.BuildWebBrowserDocument(responseList);
            var messageViewForm = new MessageViewForm(html, tx.Message);
            messageViewForm.ShowDialog();
        }
    }
}
