using System.Drawing;
using System.Windows.Forms;

namespace AskMonaViewer.Settings
{
    public class DialogSettings
    {
        public Size Size { get; set; }
        public Point Location { get; set; }
        public FormWindowState WindowState { get; set; }

        public DialogSettings()
        {
            Size = new Size(0, 0);
            Location = new Point(0, 0);
            WindowState = FormWindowState.Normal;
        }
    }
}
