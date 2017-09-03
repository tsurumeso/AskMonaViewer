using System;
using System.Windows.Forms;

using AskMonaViewer.Settings;

namespace AskMonaViewer.SubForms
{
    public partial class OptionForm : Form
    {
        private MainForm mParent;

        public OptionForm(MainForm parent, Options options)
        {
            InitializeComponent();
            mParent = parent;
            numericUpDown1.Value = (decimal)options.FirstButtonMona;
            numericUpDown2.Value = (decimal)options.SecondButtonMona;
            numericUpDown3.Value = (decimal)options.ThirdButtonMona;
            numericUpDown4.Value = (decimal)options.ForthButtonMona;
            checkBox1.Checked = options.AlwaysSage;
            checkBox2.Checked = options.AlwaysNonAnonymous;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var option = new Options();
            option.FirstButtonMona = (double)numericUpDown1.Value;
            option.SecondButtonMona = (double)numericUpDown2.Value;
            option.ThirdButtonMona = (double)numericUpDown3.Value;
            option.ForthButtonMona = (double)numericUpDown4.Value;
            option.AlwaysSage = checkBox1.Checked;
            option.AlwaysNonAnonymous = checkBox2.Checked;
            mParent.SetOption(option);
            this.Close();
        }
    }
}
