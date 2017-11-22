using System;
using System.Windows.Forms;

using AskMonaViewer.Settings;

namespace AskMonaViewer.Dialogs
{
    public partial class SetOptionDialog : Form
    {
        private Options mOptions;

        public Options Options
        {
            get { return mOptions; }
        }

        public SetOptionDialog(Options options)
        {
            InitializeComponent();
            numericUpDown1.Value = (decimal)options.FirstButtonMona;
            numericUpDown2.Value = (decimal)options.SecondButtonMona;
            numericUpDown3.Value = (decimal)options.ThirdButtonMona;
            numericUpDown4.Value = (decimal)options.ForthButtonMona;
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = options.AlwaysNonAnonymous;
            checkBox4.Checked = options.VisibleMonaJpy;
            checkBox3.Checked = options.VisibleBtcJpy;
            mOptions = options;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            mOptions.FirstButtonMona = (double)numericUpDown1.Value;
            mOptions.SecondButtonMona = (double)numericUpDown2.Value;
            mOptions.ThirdButtonMona = (double)numericUpDown3.Value;
            mOptions.ForthButtonMona = (double)numericUpDown4.Value;
            mOptions.AlwaysSage = checkBox1.Checked;
            mOptions.AlwaysNonAnonymous = checkBox2.Checked;
            mOptions.VisibleMonaJpy = checkBox4.Checked;
            mOptions.VisibleBtcJpy = checkBox3.Checked;
            this.Close();
        }
    }
}
