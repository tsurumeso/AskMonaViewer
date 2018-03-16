using System;
using System.Collections;
using System.Windows.Forms;

namespace AskMonaViewer.Utilities
{ 
    public class ListViewItemComparer : IComparer
    {
        private int mColumn;
        private SortOrder mSortOrder;
        private ComparerMode mComparerMode;
        private ComparerMode[] mColumnModes;

        public enum ComparerMode
        {
            String,
            Integer,
            Double,
            DateTime
        };

        public int Column
        {
            set
            {
                // 現在と同じ列の時は，昇順降順を切り替える
                if (mColumn == value)
                {
                    if (mSortOrder == SortOrder.Ascending)
                        mSortOrder = SortOrder.Descending;
                    else if (mSortOrder == SortOrder.Descending)
                        mSortOrder = SortOrder.Ascending;
                }
                mColumn = value;
            }
            get
            {
                return mColumn;
            }
        }

        public SortOrder SortOrder
        {
            set
            {
                mSortOrder = value;
            }
        }

        public ComparerMode[] ColumnModes
        {
            set
            {
                mColumnModes = value;
            }
        }

        public ListViewItemComparer(int col, SortOrder ord, ComparerMode cmod)
        {
            mColumn = col;
            mSortOrder = ord;
            mComparerMode = cmod;
        }

        public ListViewItemComparer()
        {
            mColumn = 0;
            mSortOrder = SortOrder.Ascending;
            mComparerMode = ComparerMode.String;
        }

        // xがyより小さいときはマイナスの数，大きいときはプラスの数，同じときは0を返す
        public int Compare(object x, object y)
        {
            int result = 0;
            // ListViewItemの取得
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            // 並べ替えの方法を決定
            if (mColumnModes != null && mColumnModes.Length > mColumn)
                mComparerMode = mColumnModes[mColumn];

            // 並び替えの方法別に，xとyを比較する
            switch (mComparerMode)
            {
                case ComparerMode.String:
                    result = string.Compare(itemx.SubItems[mColumn].Text,
                        itemy.SubItems[mColumn].Text);
                    break;
                case ComparerMode.Integer:
                    int ia, ib;
                    int.TryParse(itemx.SubItems[mColumn].Text, out ia);
                    int.TryParse(itemy.SubItems[mColumn].Text, out ib);
                    result = ia.CompareTo(ib);
                    break;
                case ComparerMode.Double:
                    double da, db;
                    double.TryParse(itemx.SubItems[mColumn].Text, out da);
                    double.TryParse(itemy.SubItems[mColumn].Text, out db);
                    result = da.CompareTo(db);
                    break;
                case ComparerMode.DateTime:
                    result = DateTime.Compare(
                        DateTime.Parse(itemx.SubItems[mColumn].Text),
                        DateTime.Parse(itemy.SubItems[mColumn].Text));
                    break;
            }

            if (mSortOrder == SortOrder.Descending)
                result = -result;

            return result;
        }
    }
}
