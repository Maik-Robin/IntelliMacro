using System;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi;
using System.Collections.Generic;

namespace IntelliMacro.CoreCommands
{
    class KeyboardCommand : AbstractCommand
    {
        bool down, up;

        internal KeyboardCommand(string name, bool down, bool up)
            : base(name, false, !up ? "&Press key" : !down ? "&Release key" : "T&ype (press and release) key", "&Keyboard/Mouse")
        {
            this.down = down;
            this.up = up;
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Key code"),
                    new ParameterDescription(true, "Second key code"),
                    new ParameterDescription(true, "Third key code"),
                    new ParameterDescription(true, "Fourth key code"),
                    new ParameterDescription(true, "Fifth key code"),
                };
            }
        }

        public override string Description
        {
            get
            {
                if (!up)
                {
                    return "Press one or more keys or mouse buttons.\n\n" +
                        "Enter the keycodes (or key name in angle brackets) of the keys to press. " +
                        "Keys are pressed from left to right.";
                }
                else if (!down)
                {
                    return "Release one or more keys or mouse buttons.\n\n" +
                        "Enter the keycodes (or key name in angle brackets) of the keys to release. " +
                        "Keys are pressed from right to left.";
                }
                else
                {
                    return "Press and release one or more keys or mouse buttons.\n\n" +
                        "Enter the keycodes (or key name in angle brackets) of the keys. " +
                        "Keys are first pressed from left to right, then released from left to right. ";
                }
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (down)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (parameters[i] != null)
                    {
                        PerformKey(context, (int)parameters[i].Number, true);
                    }
                }
            }
            if (up)
            {
                for (int i = 4; i >= 0; i--)
                {
                    if (parameters[i] != null)
                    {
                        PerformKey(context, (int)parameters[i].Number, false);
                    }
                }
            }
            SetDelay(1);
            return null;
        }

        private void PerformKey(MacroContext context, int keycode, bool pushDown)
        {
            context.InputEmulator.PerformKey(keycode, pushDown);
        }
    }

    class SendKeysCommand : AbstractCommand
    {
        bool escaped;
        internal SendKeysCommand(bool escaped)
            : base(escaped ? "SendText" : "SendKeys", false, escaped ? "Send &Text" : "Send &Keys", "&Keyboard/Mouse")
        {
            this.escaped = escaped;
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, escaped? "text":"keys"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return (escaped ? "Send a text to an application by pressing individual keys.\n" : "Send keys to an application individually\n") +
                    "This function is more error-prone than other key sending commands, and much slower than using the clipboard, so avoid it if possible.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            string keys = parameters[0].String;
            if (escaped)
                keys = SendKeysEscaper.Instance.escape(keys, false);
            using (LockKeyResetter r = new LockKeyResetter())
            {
                SendKeys.SendWait(keys);
            }
            SetDelay(1);
            return null;
        }
    }

    class VirtualInputCommand : AbstractCommand
    {
        internal VirtualInputCommand() : base("VirtualInput", false, "Toggle &virtual input", "&Keyboard/Mouse") { }

        public override string Description
        {
            get
            {
                return "Switch to or from virtual input.\n\n" +
                    "Input ID 0 is for physical input, which will move the mouse and create input events.\n" +
                    "Positive input IDs are virtual input; each of them will keep track of its own virtual foreground " +
                    "window, pressed keys and mouse position, and it will try to inject window messages to emulate input.\n" +
                    "A negative ID will create a fresh input emulator with empty state and store it at the positive ID number.\n"+
                    "This feature is experimental and might not work as expected, but it is possible to do something else while a macro is running.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Input ID"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int index = (int)parameters[0].Number;
            context.InputEmulator = GetInstance(Math.Abs(index));
            if (index < 0)
            {
                context.InputEmulator = emulators[Math.Abs(index)] = new VirtualInputEmulator();
            }
            SetDelay(1);
            return null;
        }

        List<InputEmulator> emulators = new List<InputEmulator>() { PhysicalInputEmulator.Instance };

        private InputEmulator GetInstance(int index)
        {
            while (emulators.Count <= index)
            {
                emulators.Add(new VirtualInputEmulator());
            }
            return emulators[index];
        }
    }
}