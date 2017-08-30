using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;
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

        public static string CompressString(string source)
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

        public static string DecompressString(string source)
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
    }
}
