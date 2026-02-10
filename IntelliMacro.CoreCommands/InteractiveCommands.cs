using System;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi;

namespace IntelliMacro.CoreCommands
{
    class BlockInputCommand : AbstractCommand
    {
        internal BlockInputCommand() : base("BlockInput", false, "&Block user input", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Block user input to prevent accidental disruptions of the macro.\n\n" +
                    "User input can be unblocked by pressing Ctrl+Alt+Delete.\n" +
                    "Use this command if you need more control than the identically named menu option provides.\n" +
                    "Note that some commands (like WaitForKey or Confirm) disable the input block until they are completed.";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Enabled")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0].Number == 0)
            {
                if (context.InputBlocker != null)
                {
                    context.InputBlocker.Dispose();
                    context.InputBlocker = null;
                }
            }
            else
            {
                if (context.InputBlocker == null)
                {
                    context.InputBlocker = new InputBlocker();
                }
            }
            SetDelay(1);
            return null;
        }
    }

    class MessageCommand : InteractiveCommand
    {
        internal MessageCommand() : base("Msg", false, "Show &Message", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Show a message to the user.\n" +
                    "If HintType is 1, show a hint window on top of the screen instead, or on bottom if it is 2.\n" +
                    "If HintType is 3, show a tray bubble.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Message"),
                    new ParameterDescription(true, "Title"),
                    new ParameterDescription(true, "HintType"),
                };
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            SetDelay(50);
            if (parameters[2] != null && parameters[2].IsNumber)
            {
                switch (parameters[2].Number)
                {
                    case 1: // Hint on top
                        HintWindow.Instance.Show(parameters[0].String, false);
                        return null;
                    case 2: // Hint on bottom
                        HintWindow.Instance.Show(parameters[0].String, true);
                        return null;
                    case 3: // Tray bubble
                        HintWindow.Instance.ShowBubble(parameters[0].String, parameters[1] == null ? "IntelliMacro.NET" : parameters[1].String);
                        return null;
                }
            }
            MessageBox.Show(parameters[0].String, parameters[1] == null ? "IntelliMacro.NET" : parameters[1].String, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MB_SYSTEMMODAL, false);
            return null;
        }
    }

    class ConfirmCommand : InteractiveCommand
    {
        internal ConfirmCommand() : base("Confirm", true, "Show Confirmation &Dialog", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Show a message to the user and return the clicked button.\n\n" +
                    "The second parameter must be a string of the following letters:\n" +
                    "y: Yes button\n" +
                    "n: No button\n" +
                    "a: Abort button\n" +
                    "r: Retry button\n" +
                    "i: Ignore button\n" +
                    "o: OK button\n" +
                    "c: Cancel button\n\n" +
                    "Use an uppercase letter to give the default button.\n" +
                    "Add the following symbols to the beginning to give the icon:\n" +
                    "!: Exclamation, ?: Question, i: Information, x: Error";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Message"),
                    new ParameterDescription(false, "Buttons"),
                    new ParameterDescription(true, "Title"),
                };
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            MessageBoxButtons buttons;
            MessageBoxIcon icon;
            MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1;
            string btns = parameters[1].String;
            if (btns == "") btns = " ";
            switch (btns[0])
            {
                case '?': icon = MessageBoxIcon.Question; break;
                case '!': icon = MessageBoxIcon.Exclamation; break;
                case 'i':
                case 'I': icon = MessageBoxIcon.Information; break;
                case 'x':
                case 'X': icon = MessageBoxIcon.Error; break;
                default: btns = " " + btns; icon = MessageBoxIcon.None; break;
            }
            switch (btns.Substring(1).ToLowerInvariant())
            {
                case "o": buttons = MessageBoxButtons.OK; break;
                case "oc": buttons = MessageBoxButtons.OKCancel; break;
                case "yn": buttons = MessageBoxButtons.YesNo; break;
                case "ync": buttons = MessageBoxButtons.YesNoCancel; break;
                case "ari": buttons = MessageBoxButtons.AbortRetryIgnore; break;
                case "rc": buttons = MessageBoxButtons.RetryCancel; break;
                default:
                    throw new MacroErrorException("Unsupported button combination");
            }
            for (int i = 1; i < btns.Length; i++)
            {
                if (btns.Substring(i, 1) == btns.Substring(i, 1).ToUpperInvariant())
                {
                    defaultButton = MessageBoxDefaultButton.Button1 + 256 * (i - 1);
                    break;
                }
            }
            DialogResult dlgresult = MessageBox.Show(parameters[0].String, parameters[2] == null ? "IntelliMacro.NET" : parameters[2].String, buttons, icon, defaultButton, MB_SYSTEMMODAL, false);
            string result;
            switch (dlgresult)
            {
                case DialogResult.Abort: result = "a"; break;
                case DialogResult.Cancel: result = "c"; break;
                case DialogResult.Ignore: result = "i"; break;
                case DialogResult.No: result = "n"; break;
                case DialogResult.OK: result = "o"; break;
                case DialogResult.Retry: result = "r"; break;
                case DialogResult.Yes: result = "y"; break;
                default: result = ""; break;
            }
            SetDelay(50);
            return result;
        }
    }

    class KeyPressedCommand : InteractiveCommand
    {
        internal KeyPressedCommand() : base("KeyPressed", true, "Check if &key pressed", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Check if a key has been pressed since last call.\n\n" +
                    "Use the keycode of the key as a parameter. The default value is &lt;BREAK&gt;.\n" +
                    "If the parameter is 0, return an array of all keys, if it is -1, reset the status for all keys.\n" +
                    "You may use hotkey handles (from RegisterHotkey) or other macro events instead of keycodes in this function as well.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[] { new ParameterDescription(true, "Key code") }; }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            Keys keycode = parameters[0] == null ? Keys.Pause : (Keys)parameters[0].Number;
            if ((int)keycode == 0 || (int)keycode == -1)
            {
                int[] results = new int[256];
                bool pressed = false;
                for (int i = 0; i < results.Length; i++)
                {
                    results[i] = new KeyboardKey((Keys)i).AsyncState;
                    if (results[i] != 0) pressed = true;
                }
                if ((int)keycode == -1) return pressed ? MacroObject.ONE : MacroObject.ZERO;
                MacroObject[] result = new MacroObject[256];
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = results[i];
                }
                return new MacroList(result);
            }
            else if (MacroWrappedObject.Unwrap(parameters[0]) is IMacroEvent)
            {
                IMacroEvent evt = (IMacroEvent)MacroWrappedObject.Unwrap(parameters[0]);
                if (evt != null && evt.HasOccurred)
                {
                    evt.ClearOccurred();
                    return MacroObject.ONE;
                }
                return MacroObject.ZERO;
            }
            else
            {
                return new KeyboardKey(keycode).AsyncState;
            }
        }
    }

    class WaitForKeyCommand : InteractiveCommand
    {
        internal WaitForKeyCommand() : base("WaitForKey", false, "&Wait for key press", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Wait until a specific key is pressed.\n\n" +
                    "Use the keycode of the key as a parameter. The default value is &lt;BREAK&gt;.\n" +
                    "The macro sleeps until the given key is pressed. When invoked with two arguments, the second argument sets the delay for checking the key.\n" +
                    "You may use hotkey handles (from RegisterHotkey) or other macro events instead of keycodes in this function as well.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "Key code"),
                    new ParameterDescription(true, "Delay")
                };
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            Keys keycode = parameters[0] == null ? Keys.Pause : (Keys)parameters[0].Number;
            int delay = parameters[1] == null ? 100 : Math.Abs((int)parameters[1].Number);
            if (delay == 0) delay = 1;
            bool pressed;
            IMacroEvent evt = null;
            if (MacroWrappedObject.Unwrap(parameters[0]) is IMacroEvent)
            {
                evt = (IMacroEvent)MacroWrappedObject.Unwrap(parameters[0]);
                pressed = evt != null && evt.HasOccurred;
                if (pressed) evt.ClearOccurred();
            }
            else
            {
                pressed = new KeyboardKey(keycode).AsyncState != 0;
            }
            if (pressed)
            {
                SetDelay(1);
            }
            else if (evt != null && parameters[1] == null)
            {
                SetWaitEvent(evt);
            }
            else
            {
                SetDelay(-delay);
            }
            return null;
        }
    }

    class ConsoleCommand : AbstractCommand
    {
        public ConsoleCommand() : base("Console", false, "&Console: Configure", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "flags"),
                    new ParameterDescription(true, "width"),
                    new ParameterDescription(true, "waitForInput"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Initialize the IntelliMacro.NET console, show/hide it or change its apperance.\n" +
                    "When flags is -1, the console is cleared, hidden, and reset to default settings.\n" +
                    "The console is hidden when flags is 0, or shown when 1.\n" +
                    "When flags is a string, the console is shown and configured depending on its characters:\n" +
                    "'i': show input box; 'm': maximized, 'l': at left screen border, 'r': at right screen border, 't': always on top.\n" +
                    "When width is given, the console width is adjusted.\n" +
                    "When waitForInput is 1, wait until at least one line is in the console buffer.";
            }
        }


        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0] != null)
            {
                ConsoleWindow.ConsoleFlags flags;
                if (parameters[0].IsNumber)
                {
                    flags = (ConsoleWindow.ConsoleFlags)parameters[0].Number;
                }
                else
                {
                    flags = ConsoleWindow.ConsoleFlags.Visible;
                    string flagString = parameters[0].String.ToLowerInvariant();
                    for (int i = 0; i < flagString.Length; i++)
                    {
                        switch (flagString[i])
                        {
                            case 'i': flags |= ConsoleWindow.ConsoleFlags.ShowInputBox; break;
                            case 'm': flags |= ConsoleWindow.ConsoleFlags.PositionMaximized; break;
                            case 'l': flags |= ConsoleWindow.ConsoleFlags.PositionDockLeft; break;
                            case 'r': flags |= ConsoleWindow.ConsoleFlags.PositionDockRight; break;
                            case 't': flags |= ConsoleWindow.ConsoleFlags.TopMost; break;
                        }
                    }
                }
                ConsoleWindow.Instance.ApplyFlags(flags);
            }

            if (parameters[1] != null)
            {
                ConsoleWindow.Instance.AdjustWidth((int)parameters[1].Number);
            }

            SetDelay(0);
            if (parameters[2] != null && parameters[2].Number == 1)
            {
                if (!ConsoleWindow.Instance.HasInput)
                {
                    // TODO how to handle blocked input here?
                    SetWaitEvent(ConsoleWindow.Instance.ChangeEvent);
                }
            }

            return null;
        }
    }

    class AddConsoleCommand : AbstractCommand
    {
        public AddConsoleCommand() : base("AddConsole", false, "Console: &Add content", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "text"),
                    new ParameterDescription(true, "deleteLineCount"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Write text on the console.\n" +
                    "text is the text that is written on the console, add a linebreak using the && operator if you want to start/end a new line.\n" +
                    "When deleteLineCount is present, delete that many lines before adding the text.\n" +
                    "When deleteLineCount is -1, delete everything from the console first.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int deleteLinesCount = parameters[1] != null ? (int)parameters[1].Number : 0;

            ConsoleWindow.Instance.AddText(parameters[0].String, deleteLinesCount);

            return null;
        }
    }

    class ReadConsoleCommand : AbstractCommand
    {
        public ReadConsoleCommand() : base("ReadConsole", true, "Console: &Read line", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[0]; }
        }

        public override string Description
        {
            get
            {
                return "Read one line from the console and return it\n" +
                    "If there is no input in the console buffer, return an empty line.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            return ConsoleWindow.Instance.ReadLine();
        }
    }

    class FileDialogCommand : InteractiveCommand
    {
        internal FileDialogCommand() : base("FileDialog", true, "Show &File Selection Dialog", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "title"),
                    new ParameterDescription(true, "mask"),
                    new ParameterDescription(true, "multiselect"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Show a file selection dialog.\n\n" +
                    "title is the text shown in the title bar.\n" +
                    "mask is a list of file masks, separated by pipe symbols.\n" +
                    "If multiselect is 1, more than one file can be selected; the return value will be a list then.";
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = parameters[0].String;
            try
            {
                if (parameters[1] != null)
                    ofd.Filter = parameters[1].String;
            }
            catch (ArgumentException ex)
            {
                throw new MacroErrorException("Invalid filter: " + ex.Message);
            }
            if (parameters[2] != null && parameters[2].Number == 1)
            {
                ofd.Multiselect = true;
            }
            ofd.FileName = "";
            if (ofd.ShowDialog(DummyWindow) != DialogResult.OK)
                return ofd.Multiselect ? new MacroList(new MacroObject[0]) : MacroObject.EMPTY;
            if (ofd.Multiselect)
            {
                return new MacroList(Array.ConvertAll<string, MacroObject>(ofd.FileNames, f => f));
            }
            else
            {
                return ofd.FileName;
            }
        }
    }

    class InputCommand : InteractiveCommand
    {
        internal InputCommand() : base("Input", true, "Show &Input Dialog", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "prompt"),
                    new ParameterDescription(true, "default"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Show an input dialog.\n\n" +
                    "prompt is the text shown in front of the input box.\n" +
                    "default is the default value.\n";
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            return InputBox.Show(DummyWindow, parameters[0].String, parameters[1] == null ? "" : parameters[1].String) ?? "";
        }
    }
    class SelectCommand : InteractiveCommand
    {
        public SelectCommand() : base("Select", true, "&Select from a list", "&Interaction") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[] { new ParameterDescription(false, "list") }; }
        }

        public override string Description
        {
            get
            {
                return "Show a selection list on the screen and let the user select an item of the list.\n\n" +
                    "If the user cancels the selection, the return value is 0, else it is the index of the selection.";
            }
        }

        protected override MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters)
        {
            string[] elements = new string[parameters[0].Length];
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i] = parameters[0][i + 1].String;
            }
            return SelectWindow.Show(DummyWindow, elements);
        }
    }
}
