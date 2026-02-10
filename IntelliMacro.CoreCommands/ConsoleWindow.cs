using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    internal partial class ConsoleWindow : Form
    {
        [Flags]
        internal enum ConsoleFlags : int
        {
            Visible = 1,
            ShowInputBox = 2,
            PositionMaximized = 4,
            PositionDockLeft = 8,
            PositionDockRight = 12,
            PositionFlags = 12,
            TopMost = 16
        }

        private static ConsoleWindow instance;

        internal static ConsoleWindow Instance
        {
            get
            {
                if (instance == null)
                    instance = new ConsoleWindow();
                return instance;
            }
        }

        ConsoleEvent changeEvent = new ConsoleEvent();
        List<string> consoleBuffer = new List<string>();

        internal ConsoleWindow()
        {
            InitializeComponent();
        }

        internal void ApplyFlags(ConsoleFlags flags)
        {
            if ((int)flags == -1)
            {
                this.Dispose();
                instance = null;
                return;
            }
            inputBox.Visible = (flags & ConsoleFlags.ShowInputBox) != 0;
            if ((flags & ConsoleFlags.Visible) != 0)
            {
                if (!Visible)
                {
                    Show();
                    SystemWindow.ForegroundWindow = new SystemWindow(this);
                }
            }
            else
            {
                Hide();
            }
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            if ((flags & ConsoleFlags.PositionFlags) == ConsoleFlags.PositionMaximized)
            {
                this.WindowState = FormWindowState.Maximized;
            }
            else if ((flags & ConsoleFlags.PositionFlags) == ConsoleFlags.PositionDockLeft)
            {
                this.WindowState = FormWindowState.Normal;
                this.Top = workArea.Top;
                this.Left = workArea.Left;
                this.Height = workArea.Height;
                this.Width = workArea.Width / 4;
            }
            else if ((flags & ConsoleFlags.PositionFlags) == ConsoleFlags.PositionDockRight)
            {
                this.WindowState = FormWindowState.Normal;
                this.Top = workArea.Top;
                this.Left = workArea.Left + workArea.Width * 3 / 4;
                this.Height = workArea.Height;
                this.Width = workArea.Width / 4;
            }
            TopMost = (flags & ConsoleFlags.TopMost) != 0;
        }

        internal void AdjustWidth(int newWidth)
        {
            if (newWidth < 200) newWidth = 200;
            if (WindowState != FormWindowState.Normal) return;
            if (Left != Screen.PrimaryScreen.WorkingArea.Left)
                Left -= (newWidth - Width);
            Width = newWidth;
        }

        internal void AddText(string textToAdd, int deleteLinesCount)
        {
            String txt = outputBox.Text;
            if (deleteLinesCount == -1)
                txt = "";
            else if (deleteLinesCount > 0)
            {
                string endNewLine = "";
                if (txt.EndsWith(Environment.NewLine))
                {
                    txt = txt.Substring(0, txt.Length - Environment.NewLine.Length);
                    endNewLine = Environment.NewLine;
                }
                for (int i = 0; i < deleteLinesCount; i++)
                {
                    int j = txt.LastIndexOf(Environment.NewLine);
                    if (j == -1)
                    {
                        txt = endNewLine = "";
                        break;
                    }
                    txt = txt.Substring(0, j);
                }
                txt += endNewLine;
            }
            txt += textToAdd;
            outputBox.Text = txt;
            outputBox.Select(txt.Length, 0);
            outputBox.ScrollToCaret();
        }

        public string ReadLine()
        {
            if (consoleBuffer.Count == 0) return "";
            string result = consoleBuffer[0];
            consoleBuffer.RemoveAt(0);
            return result;
        }

        public bool HasInput { get { return consoleBuffer.Count > 0; } }

        public ConsoleEvent ChangeEvent { get { return changeEvent; } }

        private void inputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                consoleBuffer.Add(inputBox.Text);
                inputBox.Text = "";
                changeEvent.Changed();
                e.Handled = true;
            }
        }

        private void ConsoleWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            instance = null;
        }
    }

    class ConsoleEvent : AbstractMacroEvent
    {
        public override void Dispose() { }
        internal void Changed() { FireEvent(); }
    }
}
