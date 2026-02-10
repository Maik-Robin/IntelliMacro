using System;
using System.Collections.Generic;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi;

namespace IntelliMacro.CoreCommands
{
    class MacroHotkey : Hotkey, IMacroEvent
    {
        bool pressed = false;

        public event EventHandler Occurred;

        internal static List<MacroHotkey> AllHotkeys = new List<MacroHotkey>();

        internal MacroHotkey(Keys keycode, bool control, bool alt, bool shift, bool window)
        {
            this.Ctrl = control;
            this.Alt = alt;
            this.Shift = shift;
            this.WindowsKey = window;
            this.KeyCode = keycode;
            this.HotkeyPressed += new EventHandler(MacroHotkey_HotkeyPressed);
            AllHotkeys.Add(this);
            try
            {
                this.Enabled = true;
            }
            catch (HotkeyAlreadyInUseException)
            {
                throw new MacroErrorException("Hotkey already in use");
            }
        }

        void MacroHotkey_HotkeyPressed(object sender, EventArgs e)
        {
            pressed = true;
            if (Occurred != null) Occurred(this, EventArgs.Empty);
            lock (this)
            {
                System.Threading.Monitor.PulseAll(this);
            }
        }

        public bool HasOccurred { get { return pressed; } }

        public void ClearOccurred() { pressed = false; }

        public void WaitFor()
        {
            lock (this)
            {
                while (!pressed)
                {
                    System.Threading.Monitor.Wait(this);
                }
                pressed = false;
            }
        }
    }

    class RegisterHotkeyCommand : AbstractCommand
    {
        internal RegisterHotkeyCommand() : base("RegisterHotkey", true, "Register &hotkey", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Register a global hotkey.\n\n" +
                    "All key codes except the last one have to be CTRL or ALT or SHIFT or WINDOW\n" +
                    "The return value is a hotkey handle, that can be used for the KeyPressed and WaitForKey commands.\n" +
                    "It can also be used as an event in the IntelliMacro Scheduler.";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Key code 1"),
                    new ParameterDescription(true, "Key code 2"),
                    new ParameterDescription(true, "Key code 3"),
                    new ParameterDescription(true, "Key code 4"),
                    new ParameterDescription(true, "Key code 5"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            Keys mainKey = (Keys)parameters[0].Number;
            bool control = false, alt = false, shift = false, window = false;
            for (int i = 1; i < parameters.Length; i++)
            {
                if (parameters[i] == null) continue;
                switch (mainKey)
                {
                    case Keys.ControlKey:
                        control = true; break;
                    case Keys.ShiftKey:
                        shift = true; break;
                    case Keys.Menu:
                        alt = true; break;
                    case Keys.LWin:
                    case Keys.RWin:
                        window = true; break;
                    default:
                        throw new MacroErrorException("Invalid modifier key: " + KeyNames.GetName((int)mainKey));
                }
                mainKey = (Keys)parameters[i].Number;
            }
            MacroHotkey hk = new MacroHotkey(mainKey, control, alt, shift, window);
            return new MacroWrappedObject(hk);
        }

        private bool GetBool(MacroObject value)
        {
            return value != null && value.Number != 0;
        }
    }

    class UnregisterHotkeyCommand : AbstractCommand
    {

        internal UnregisterHotkeyCommand() : base("UnregisterHotkey", false, "&Unregister hotkey", "&Interaction") { }

        public override string Description
        {
            get
            {
                return "Unregister a registered hotkey. Without arguments, unregister all hotkeys ever registered.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "Hotkey handle")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0] == null)
            {
                foreach (MacroHotkey hk in MacroHotkey.AllHotkeys)
                {
                    hk.Dispose();
                }
                MacroHotkey.AllHotkeys.Clear();
            }
            else
            {
                MacroHotkey hk = MacroWrappedObject.Unwrap(parameters[0]) as MacroHotkey;
                if (hk != null)
                    hk.Dispose();
            }
            return null;
        }
    }
}