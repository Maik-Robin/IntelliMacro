using System;
using System.Drawing;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    partial class HintWindow : Form
    {
        static HintWindow instance = null;

        internal static HintWindow Instance
        {
            get
            {
                if (instance == null)
                    instance = new HintWindow();
                return instance;
            }
        }

        private HintWindow()
        {
            InitializeComponent();
        }

        internal void Show(string text, bool bottom)
        {
            Hide();
            if (text == "")
                return;
            int width = Screen.PrimaryScreen.WorkingArea.Width - 100;
            Graphics g = Graphics.FromHwnd(label.Handle);
            int height = (int)g.MeasureString(text, label.Font, width).Height + 6;
            g.Dispose();
            label.Text = text;
            this.Width = width;
            this.Height = height;
            this.Left = 50;
            this.Top = bottom ? Screen.PrimaryScreen.WorkingArea.Height - height - 2 : 2;
            SystemWindow fg = SystemWindow.ForegroundWindow;
            Show();
            this.Width = width;
            this.Height = height;
            SystemWindow.ForegroundWindow = fg;
        }

        internal void ShowBubble(string text, string title)
        {
            notifyIcon.Visible = false;
            if (text == "") return;
            Utilities.InvokeLater(delegate
            {
                notifyIcon.Visible = true;
                notifyIcon.ShowBalloonTip(100000, title, text, ToolTipIcon.Info);
            });
        }

        private void notifyIcon_BalloonTipClosed(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
        }
    }
}
