using System;
using System.Drawing;
using System.Windows.Forms;

namespace AskMonaViewer.Utilities
{
    public class Common
    {
        public static string Digits(double value)
        {
            string valueString = value.ToString("F8").TrimEnd('0');

            int index = valueString.IndexOf('.');
            if (index == -1)
                return value.ToString("F0");

            return value.ToString(String.Format("F{0}", valueString.Substring(index + 1).Length));
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(unixTimeStamp).ToLocalTime();
        }

        public static int DateTimeToUnixTimeStamp(DateTime dateTime)
        {
            return (int)(dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime()).TotalSeconds;
        }

        public static void UpdateColumnColors(ListViewEx listView, Color odd, Color even)
        {
            bool flag = true;
            foreach (ListViewItem lvi in listView.Items)
            {
                if (flag)
                    lvi.BackColor = odd;
                else
                    lvi.BackColor = even;
                flag = !flag;
            }
        }
    }
}
