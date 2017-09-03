using System.Drawing;
using System.Windows.Forms;

namespace AskMonaViewer.Settings
{
    public class FormSettings
    {
        public Size Size { get; set; }
        public Point Location { get; set; }
        public FormWindowState WindowState { get; set; }

        public FormSettings()
        {
            Size = new Size(0, 0);
            Location = new Point(0, 0);
            WindowState = FormWindowState.Normal;
        }
    }
}
