using System.Collections.Generic;
using System.Windows.Forms;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// This class handles mapping between key names and key codes.
    /// </summary>
    public static class KeyNames
    {
        static string[] names = null;
        static Dictionary<string, int> nameMap = new Dictionary<string, int>();

        private static void Init()
        {
            if (names != null) return;
            names = new string[256];
            for (int i = 0; i < 26; i++)
            {
                names[(int)(Keys.A + i)] = "" + (char)('A' + i);
            }
            for (int i = 0; i < 10; i++)
            {
                names[(int)(Keys.D0 + i)] = "" + (char)('0' + i);
                names[(int)(Keys.NumPad0 + i)] = "NUM" + (char)('0' + i);
            }
            for (int i = 0; i < 24; i++)
            {
                names[(int)(Keys.F1 + i)] = "F" + (i + 1);
            }

            names[(int)Keys.ControlKey] = "CTRL";
            names[(int)Keys.LControlKey] = "CTRL-L";
            names[(int)Keys.RControlKey] = "CTRL-R";
            names[(int)Keys.Menu] = "ALT";
            names[(int)Keys.LMenu] = "ALT-L";
            names[(int)Keys.RMenu] = "ALT-R";
            names[(int)Keys.ShiftKey] = "SHIFT";
            names[(int)Keys.LShiftKey] = "SHIFT-L";
            names[(int)Keys.RShiftKey] = "SHIFT-R";

            names[(int)Keys.LButton] = "MOUSE-L";
            names[(int)Keys.RButton] = "MOUSE-R";
            names[(int)Keys.MButton] = "MOUSE-M";
            names[(int)Keys.Return] = "RETURN";
            names[(int)Keys.Up] = "UP";
            names[(int)Keys.Down] = "DOWN";
            names[(int)Keys.Left] = "LEFT";
            names[(int)Keys.Right] = "RIGHT";
            names[(int)Keys.Home] = "HOME";
            names[(int)Keys.End] = "END";
            names[(int)Keys.PageUp] = "PGUP";
            names[(int)Keys.PageDown] = "PGDN";
            names[(int)Keys.Add] = "NUM+";
            names[(int)Keys.Space] = "SPACE";
            names[(int)Keys.Back] = "BS";
            names[(int)Keys.Cancel] = "CANCEL";
            names[(int)Keys.Capital] = "CAPS-LOCK";
            names[(int)Keys.Clear] = "CLEAR";
            names[(int)Keys.Delete] = "DEL";
            names[(int)Keys.Decimal] = "NUM.";
            names[(int)Keys.Divide] = "NUM/";
            names[(int)Keys.Escape] = "ESC";
            names[(int)Keys.Execute] = "EXEC";
            names[(int)Keys.Help] = "HELP";
            names[(int)Keys.Insert] = "INS";
            names[(int)Keys.Multiply] = "NUM*";
            names[(int)Keys.NumLock] = "NUM-LOCK";
            names[(int)Keys.Pause] = "BREAK";
            names[(int)Keys.Print] = "PRTSC";
            names[(int)Keys.Scroll] = "SCROLL-LOCK";
            names[(int)Keys.Select] = "SELECT";
            names[(int)Keys.Separator] = "NUM-RETURN";
            names[(int)Keys.Snapshot] = "SNAPSHOT";
            names[(int)Keys.Subtract] = "NUM-";
            names[(int)Keys.Tab] = "TAB";
            names[(int)Keys.LWin] = "WIN";
            names[(int)Keys.RWin] = "RWIN";
            names[(int)Keys.Apps] = "CONTEXT";
            names[(int)Keys.OemPipe] = "PIPE";
            names[(int)Keys.OemOpenBrackets] = "OPENBRACKETS";
            names[(int)Keys.OemCloseBrackets] = "CLOSEBRACKETS";
            names[(int)Keys.OemSemicolon] = "SEMICOLON";
            names[(int)Keys.Oemplus] = "PLUS";
            names[(int)Keys.Oemcomma] = "COMMA";
            names[(int)Keys.Oemtilde] = "TILDE";
            names[(int)Keys.OemQuotes] = "QUOTES";
            names[(int)Keys.OemQuestion] = "QUESTION";
            names[(int)Keys.OemBackslash] = "BACKSLASH";
            names[(int)Keys.BrowserBack] = "BACK";
            names[(int)Keys.BrowserForward] = "FORWARD";
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != null)
                    nameMap.Add(names[i].ToUpperInvariant(), i);
            }
        }

        /// <summary>
        /// Return the key name of a given key code.
        /// </summary>
        public static string GetName(int keyCode)
        {
            Init();
            if (names[keyCode] != null) return names[keyCode];
            return "" + keyCode;
        }

        /// <summary>
        /// Return the key code of a given key name.
        /// </summary>
        public static int GetCode(string keyName)
        {
            Init();
            if (nameMap.ContainsKey(keyName.ToUpperInvariant())) return nameMap[keyName.ToUpperInvariant()];
            int result;
            if (int.TryParse(keyName, out result)) return result;
            throw new MacroErrorException("Unknown key name: " + keyName);
        }
    }
}
