using System.Windows.Forms;

using AskMonaViewer.Settings;

namespace AskMonaViewer.Utilities
{
    public class FormEx : Form
    {
        public FormSettings SaveSettings()
        {
            var settings = new FormSettings();
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

        public void LoadSettings(FormSettings settings)
        {
            this.Size = settings.Size;
            this.Location = settings.Location;
            this.WindowState = settings.WindowState;
        }
    }
}
