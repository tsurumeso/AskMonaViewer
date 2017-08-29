using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using AskMonaViewer.Api;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.SubForms
{
    public partial class TransactionViewForm : Form
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private ListViewItemComparer mListViewItemSorterDW;
        private ListViewItemComparer mListViewItemSorterRS;
        private TransactionViewFormSettings mSettings;

        public TransactionViewForm(MainForm parent, AskMonaApi api)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mSettings = new TransactionViewFormSettings();
            mListViewItemSorterDW = new ListViewItemComparer();
            mListViewItemSorterDW.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.DateTime,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Double
            };
            mListViewItemSorterRS = new ListViewItemComparer();
            mListViewItemSorterRS.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.Integer,
                ListViewItemComparer.ComparerMode.DateTime,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Double
            };
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
                            Common.UnixTimeStampToDateTime(txs[i].Created).ToString(),
                            txs[i].Item == "deposit" ? "入金" : "出金",
                            (Double.Parse(txs[i].Amount) / 100000000).ToString("F8")
                        }
                    );
                    listViewEx1.Items.Add(lvi);
                }
                listViewEx1.ListViewItemSorter = mListViewItemSorterDW;
                Common.UpdateColumnColors(listViewEx1, Color.White, Color.Lavender);
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
                            Common.UnixTimeStampToDateTime(txs[i].Created).ToString(),
                            txs[i].Item == "receive" ? "受け取り" : "ばらまき",
                            txs[i].User != null ? txs[i].User.UserName + txs[i].User.UserDan : "匿名",
                            (Double.Parse(txs[i].Amount) / 100000000).ToString("F8")
                        }
                    );
                    lvi.Tag = txs[i];
                    listViewEx2.Items.Add(lvi);
                }
                listViewEx2.ListViewItemSorter = mListViewItemSorterRS;
                Common.UpdateColumnColors(listViewEx2, Color.White, Color.Lavender);
                listViewEx2.EndUpdate();
            }
        }

        private void listViewEx1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorterDW.Column = e.Column;
            listViewEx1.Sort();
            Common.UpdateColumnColors(listViewEx1, Color.White, Color.Lavender);
            for (int i = 0; i < listViewEx1.Items.Count; i++)
                listViewEx1.Items[i].SubItems[0].Text = (i + 1).ToString();
        }

        private void listViewEx2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorterRS.Column = e.Column;
            listViewEx2.Sort();
            Common.UpdateColumnColors(listViewEx2, Color.White, Color.Lavender);
            for (int i = 0; i < listViewEx2.Items.Count; i++)
                listViewEx2.Items[i].SubItems[0].Text = (i + 1).ToString();
        }

        private async void listViewEx2_DoubleClick(object sender, EventArgs e)
        {
            var tx = (Transaction)listViewEx2.SelectedItems[0].Tag;
            var responseList = await mApi.FetchResponseListAsync(tx.TopicId, tx.ResponceId, tx.ResponceId, 1);
            var html = await mParent.BuildWebBrowserDocument(responseList);
            var messageViewForm = new MessageViewForm(html, tx.Message, responseList.Topic.Title);
            messageViewForm.LoadSettings(mSettings.MessageViewFormSettings);
            messageViewForm.ShowDialog();
            mSettings.MessageViewFormSettings = messageViewForm.SaveSettings();
        }

        public TransactionViewFormSettings SaveSettings()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                mSettings.Size = this.Bounds.Size;
                mSettings.Location = this.Bounds.Location;
            }
            else
            {
                mSettings.Size = this.RestoreBounds.Size;
                mSettings.Location = this.RestoreBounds.Location;
            }
            mSettings.WindowState = this.WindowState;
            return mSettings;
        }

        public void LoadSettings(TransactionViewFormSettings settings)
        {
            this.Size = settings.Size;
            this.Location = settings.Location;
            this.WindowState = settings.WindowState;
            this.mSettings = settings;
        }
    }
}
