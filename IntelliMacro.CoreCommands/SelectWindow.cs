using System;
using System.Windows.Forms;

namespace IntelliMacro.CoreCommands
{
    partial class SelectWindow : Form
    {
        int result = 0;

        private SelectWindow()
        {
            InitializeComponent();
        }

        private void list_DoubleClick(object sender, EventArgs e)
        {
            if (list.SelectedIndex == -1) return;
            result = list.SelectedIndex + 1;
            Dispose();
        }

        private void ok_Click(object sender, EventArgs e)
        {
            if (list.SelectedIndex == -1) return;
            result = list.SelectedIndex + 1;
            Dispose();
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            result = 0;
            Dispose();
        }

        public static int Show(Form owner, string[] list)
        {
            SelectWindow sw = new SelectWindow();
            sw.list.Items.AddRange(list);
            sw.ShowDialog(owner);
            return sw.result;
        }
    }
}
