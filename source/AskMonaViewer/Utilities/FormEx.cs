using System.Windows.Forms;

using AskMonaViewer.Settings;

namespace AskMonaViewer.Utilities
{
    public class FormEx : Form
    {
        public DialogSettings SaveSettings()
        {
            var settings = new DialogSettings();
            if (this.WindowState == FormWindowState.Normal)
            {
                settings.Size = this.Bounds.Size;
                settings.Location = this.Bounds.Location;
            }
            else
            {
                settings.Size = this.RestoreBounds.Size;
                settings.Location = this.RestoreBounds.Location;
            }
            settings.WindowState = this.WindowState;
            return settings;
        }

        public void LoadSettings(DialogSettings settings)
        {
            this.Size = settings.Size;
            this.Location = settings.Location;
            this.WindowState = settings.WindowState;
        }
    }
}
