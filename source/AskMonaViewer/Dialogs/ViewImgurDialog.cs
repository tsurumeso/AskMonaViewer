using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using AskMonaViewer.Utilities;

namespace AskMonaViewer.Dialogs
{
    public partial class ViewImgurDialog : FormEx
    {
        private ImgurApi mImgurApi;
        private ListViewItemComparer mListViewItemSorter;
        public List<ImgurImage> ImgurImageList { get; }

        public ViewImgurDialog(ImgurApi api, List<ImgurImage> imgurImageList)
        {
            InitializeComponent();
            mImgurApi = api;
            ImgurImageList = imgurImageList;

            mListViewItemSorter = new ListViewItemComparer();
            mListViewItemSorter.ColumnModes = new ListViewItemComparer.ComparerMode[]
            {
                ListViewItemComparer.ComparerMode.DateTime,
                ListViewItemComparer.ComparerMode.String,
            };

            listViewEx1.BeginUpdate();
            foreach (var imgurImage in imgurImageList)
            {
                var lvi = new ListViewItem(
                    new string[] {
                        Common.UnixTimeStampToDateTime(imgurImage.DateTime).ToString(),
                        imgurImage.Link
                    }
                );
                lvi.Tag = imgurImage;
                listViewEx1.Items.Add(lvi);
            }
            listViewEx1.ListViewItemSorter = mListViewItemSorter;
            Common.UpdateColumnColors(listViewEx1, Color.White, Color.Lavender);
            listViewEx1.EndUpdate();
        }

        private void listViewEx1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

            var imgurImage = listViewEx1.SelectedItems[0].Tag as ImgurImage;
            pictureBox1.LoadAsync(imgurImage.Link);
        }

        private void listViewEx1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            mListViewItemSorter.Column = e.Column;
            listViewEx1.Sort();
            Common.UpdateColumnColors(listViewEx1, Color.White, Color.Lavender);
        }

        private void Copy_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

            var imgurImage = listViewEx1.SelectedItems[0].Tag as ImgurImage;
            Clipboard.SetText(imgurImage.Link);
        }

        private async void Delete_ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewEx1.SelectedItems.Count == 0)
                return;

            var idx = listViewEx1.SelectedIndices[0];
            var imgurImage = listViewEx1.Items[idx].Tag as ImgurImage;
            await mImgurApi.DeleteImage(imgurImage.DeleteHash);
            listViewEx1.Items.RemoveAt(idx);
            for (int i = 0; i < ImgurImageList.Count; i++)
            {
                if (ImgurImageList[i].Id == imgurImage.Id)
                {
                    ImgurImageList.RemoveAt(i);
                    break;
                }
            }
            pictureBox1.LoadAsync(imgurImage.Link);
        }
    }
}
