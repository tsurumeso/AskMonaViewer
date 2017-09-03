using System;
using System.Collections;
using System.Windows.Forms;

namespace AskMonaViewer.Utilities
{ 
    public class ListViewItemComparer : IComparer
    {
        private int _column;
        private SortOrder _order;
        private ComparerMode _mode;
        private ComparerMode[] _columnModes;

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
                if (_column == value)
                {
                    if (_order == SortOrder.Ascending)
                        _order = SortOrder.Descending;
                    else if (_order == SortOrder.Descending)
                        _order = SortOrder.Ascending;
                }
                _column = value;
            }
            get
            {
                return _column;
            }
        }

        public ComparerMode[] ColumnModes
        {
            set
            {
                _columnModes = value;
            }
        }

        public ListViewItemComparer(int col, SortOrder ord, ComparerMode cmod)
        {
            _column = col;
            _order = ord;
            _mode = cmod;
        }

        public ListViewItemComparer()
        {
            _column = 0;
            _order = SortOrder.Ascending;
            _mode = ComparerMode.String;
        }

        // xがyより小さいときはマイナスの数，大きいときはプラスの数，同じときは0を返す
        public int Compare(object x, object y)
        {
            int result = 0;
            // ListViewItemの取得
            ListViewItem itemx = (ListViewItem)x;
            ListViewItem itemy = (ListViewItem)y;

            // 並べ替えの方法を決定
            if (_columnModes != null && _columnModes.Length > _column)
                _mode = _columnModes[_column];

            // 並び替えの方法別に，xとyを比較する
            switch (_mode)
            {
                case ComparerMode.String:
                    result = string.Compare(itemx.SubItems[_column].Text,
                        itemy.SubItems[_column].Text);
                    break;
                case ComparerMode.Integer:
                    int ia, ib;
                    int.TryParse(itemx.SubItems[_column].Text, out ia);
                    int.TryParse(itemy.SubItems[_column].Text, out ib);
                    result = ia.CompareTo(ib);
                    break;
                case ComparerMode.Double:
                    double da, db;
                    double.TryParse(itemx.SubItems[_column].Text, out da);
                    double.TryParse(itemy.SubItems[_column].Text, out db);
                    result = da.CompareTo(db);
                    break;
                case ComparerMode.DateTime:
                    result = DateTime.Compare(
                        DateTime.Parse(itemx.SubItems[_column].Text),
                        DateTime.Parse(itemy.SubItems[_column].Text));
                    break;
            }

            if (_order == SortOrder.Descending)
                result = -result;

            return result;
        }
    }
}
