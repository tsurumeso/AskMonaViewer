using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;

using AskMonaWrapper;
using AskMonaViewer.Settings;
using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewTransactionsDialog : FormEx
    {
        private MainForm mParent;
        private AskMonaApi mApi;
        private ApplicationSettings mSettings;
        private ListViewItemComparer mListViewItemSorterDW;
        private ListViewItemComparer mListViewItemSorterRS;
        private ViewMessagesDialog mViewMessagesDialog = null;
        private ViewProfileDialog mViewProfileDialog = null;

        public ApplicationSettings Settings
        {
            get { return mSettings; }
        }

        public ViewTransactionsDialog(MainForm parent, AskMonaApi api, ApplicationSettings settings)
        {
            InitializeComponent();
            mParent = parent;
            mApi = api;
            mSettings = settings;
            mListViewItemSorterDW = new ListViewItemComparer();
            mListViewItemSorterDW.SortOrder = SortOrder.Descending;
            mListViewItemSorterDW.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.DateTime,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Double
            };
            mListViewItemSorterRS = new ListViewItemComparer();
            mListViewItemSorterRS.SortOrder = SortOrder.Descending;
            mListViewItemSorterRS.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.DateTime,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.String,
                ListViewItemComparer.ComparerMode.Double
            };
        }

        private async void ViewTransactionDialog_Load(object sender, EventArgs e)
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
                            Common.UnixTimeStampToDateTime(txs[i].Created).ToString(),
                            txs[i].Item == "receive" ? "受け取り " : "ばらまき ",
                            txs[i].ResponceId == 0 ? "ユーザー" : "レス",
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
        }

        private void listViewEx2_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorterRS.Column = e.Column;
            listViewEx2.Sort();
            Common.UpdateColumnColors(listViewEx2, Color.White, Color.Lavender);
        }

        private async void listViewEx2_DoubleClick(object sender, EventArgs e)
        {
            var mousePosition = listViewEx2.PointToClient(Control.MousePosition);
            var hit = listViewEx2.HitTest(mousePosition);
            var columnIndex = hit.Item.SubItems.IndexOf(hit.SubItem);

            var tx = (Transaction)listViewEx2.SelectedItems[0].Tag;
            if (columnIndex == 2 && tx.ResponceId != 0)
            {
                var responseList = await mApi.FetchResponseListAsync(tx.TopicId, tx.ResponceId, tx.ResponceId, 1);
                var html = await mParent.BuildWebBrowserDocument(responseList);
                if (mViewMessagesDialog != null)
                    mViewMessagesDialog.Close();
                mViewMessagesDialog = new ViewMessagesDialog(html, tx.Message, responseList.Topic.Title);
                mViewMessagesDialog.LoadSettings(mSettings.ViewMessagesDialogSettings);
                mViewMessagesDialog.FormClosed += OnMessagesViewDialogClosed;
                mViewMessagesDialog.Show(this);
            }
            else if (columnIndex == 3 && tx.User != null)
            {
                if (mViewProfileDialog != null)
                    mViewProfileDialog.Close();
                mViewProfileDialog = new ViewProfileDialog(mParent, mSettings.Options, mApi, tx.User.UserId);
                mViewProfileDialog.LoadSettings(mSettings.ViewProfileDialogSettings);
                mViewProfileDialog.FormClosed += OnProfileViewDialogClosed;
                mViewProfileDialog.Show(this);
                mSettings.ViewProfileDialogSettings = mViewProfileDialog.SaveSettings();
            }
        }

        private void OnMessagesViewDialogClosed(object sender, EventArgs e)
        {
            mSettings.ViewMessagesDialogSettings = mViewMessagesDialog.SaveSettings();
            mViewMessagesDialog.Dispose();
            mViewMessagesDialog = null;
        }

        private void OnProfileViewDialogClosed(object sender, EventArgs e)
        {
            mSettings.ViewProfileDialogSettings = mViewProfileDialog.SaveSettings();
            mViewProfileDialog.Dispose();
            mViewProfileDialog = null;
        }

        private void TransactionViewForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (mViewMessagesDialog != null)
                mViewMessagesDialog.Close();
            if (mViewProfileDialog != null)
                mViewProfileDialog.Close();

            return;
        }
    }
}
