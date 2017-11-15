using System;
using System.Windows.Forms;

using AskMonaViewer.Settings;

namespace AskMonaViewer.SubForms
{
    public partial class SetOptionDialog : Form
    {
        private MainForm mParent;

        public SetOptionDialog(MainForm parent, Options options)
        {
            InitializeComponent();
            mParent = parent;
            numericUpDown1.Value = (decimal)options.FirstButtonMona;
            numericUpDown2.Value = (decimal)options.SecondButtonMona;
            numericUpDown3.Value = (decimal)options.ThirdButtonMona;
            numericUpDown4.Value = (decimal)options.ForthButtonMona;
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = options.AlwaysNonAnonymous;
            checkBox4.Checked = options.VisibleMonaJpy;
            checkBox3.Checked = options.VisibleBtcJpy;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var options = new Options();
            options.FirstButtonMona = (double)numericUpDown1.Value;
            options.SecondButtonMona = (double)numericUpDown2.Value;
            options.ThirdButtonMona = (double)numericUpDown3.Value;
            options.ForthButtonMona = (double)numericUpDown4.Value;
            options.AlwaysSage = checkBox1.Checked;
            options.AlwaysNonAnonymous = checkBox2.Checked;
            options.VisibleMonaJpy = checkBox4.Checked;
            options.VisibleBtcJpy = checkBox3.Checked;
            mParent.SetOption(options);
            this.Close();
        }
    }
}
