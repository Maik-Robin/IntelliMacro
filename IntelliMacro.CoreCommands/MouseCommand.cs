using System;
using System.Drawing;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class MouseCommand : AbstractCommand
    {
        bool relative, scaled;
        internal MouseCommand(string name, bool relative, bool scaled)
            : base(name, false, name == "MouseRelCtrl" ? "Set mouse position relative to co&ntrol" : scaled ? "Set &scaled mouse position" : relative ? "Set re&lative mouse position" : "Set &absolute mouse position", "&Keyboard/Mouse")
        {
            this.relative = relative;
            this.scaled = scaled;
        }

        public override string Description
        {
            get
            {
                if (scaled)
                {
                    return "Move the mouse to a scaled position\n\n" +
                        "Coordinates are in 1/1000ths of the window size. So 0 is top/left, 1000 is bottom/right. The base window is the foreground window";
                }
                else if (relative)
                {
                    return "Move the mouse to a relative position\n\n" +
                        "Coordinates are an pixels, relative to the upper-left corner of the foreground window.";
                }
                else
                {
                    return "Move the mouse to an absolute position\n\n" +
                        "Coordinates are in absolute pixels, based on the upper-left corner of the primary monitor.\n" +
                        "Optionally, you may give a monitor number as the third parameter";
                }
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                if (relative)
                {
                    return new ParameterDescription[] {
                        new ParameterDescription(false, "X coordinate"),
                        new ParameterDescription(false, "Y coordinate")
                    };
                }
                else
                {
                    return new ParameterDescription[] {
                        new ParameterDescription(false, "X coordinate"),
                        new ParameterDescription(false, "Y coordinate"),
                        new ParameterDescription(true, "Screen number")
                    };
                }
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            SetDelay(1);
            int x = (int)parameters[0].Number;
            int y = (int)parameters[1].Number;
            if (!relative && x == -1 && y == -1)
            {
                context.MousePosition = null;
                return null;
            }
            if (relative)
            {
                Rectangle baseRectangle = GetBaseRectangle(context, parameters);
                if (scaled)
                {
                    x = x * baseRectangle.Width / 1000;
                    y = y * baseRectangle.Height / 1000;
                }
                x = x + baseRectangle.Left;
                y = y + baseRectangle.Top;
            }
            else if (parameters[2] != null)
            {
                ConvertCoordinatesForScreen((int)parameters[2].Number, ref x, ref y);
            }
            Point pos = new Point(x, y);
            context.MousePosition = pos;
            context.InputEmulator.CursorPosition = pos;
            KeyboardKey.InjectMouseEvent(1, 0, 0, 0, UIntPtr.Zero);
            return null;
        }

        protected virtual Rectangle GetBaseRectangle(MacroContext context, MacroObject[] parameters)
        {
            return context.InputEmulator.ForegroundWindow.Rectangle;
        }

        internal static void ConvertCoordinatesForScreen(int screen, ref int x, ref int y)
        {
            if (screen < 0) screen += Screen.AllScreens.Length + 1;
            if (screen >= 1 && screen <= Screen.AllScreens.Length)
            {
                Rectangle bounds = System.Windows.Forms.Screen.AllScreens[screen - 1].Bounds;
                x += bounds.X;
                y += bounds.Y;
            }
        }
    }

    class ControlBasedMouseCommand : MouseCommand
    {
        internal ControlBasedMouseCommand() : base("MouseRelCtrl", true, true) { }

        public override string Description
        {
            get
            {
                return "Move the mouse to a scaled position\n\n" +
                    "Coordinates are in 1/1000ths of the control size. So 0 is top/left, 1000 is bottom/right. " +
                    "The base control is the control specified by the window string in the third parameter";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                ParameterDescription[] pd = base.ParameterDescriptions;
                return new ParameterDescription[] {
                    pd[0],
                    pd[1],
                    new ParameterDescription(false, "Window string of control"),
                };
            }
        }
        protected override Rectangle GetBaseRectangle(MacroContext context, MacroObject[] parameters)
        {
            WindowNode wn = PathParser.ParsePath(new WindowNode(context.InputEmulator.ForegroundWindow), parameters[2].String, "control");
            if (wn == null) return new Rectangle(-1, -1, 0, 0);
            return wn.Window.Rectangle;
        }
    }

    class WheelCommand : AbstractCommand
    {
        internal WheelCommand()
            : base("Wheel", false, "Rotate mouse &wheel", "&Keyboard/Mouse") { }


        public override string Description
        {
            get
            {
                return "Rotate the mouse wheel.\n\n" +
                    "Use the parameter to specify the distance the wheel is rotated. " +
                    "One notch is 120. Positive values scroll up, negative ones scroll down.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Wheel distance")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            context.InputEmulator.RotateMouseWheel((uint)parameters[0].Number);
            SetDelay(1);
            return null;
        }
    }

    class GetMouseCommand : AbstractCommand
    {
        internal GetMouseCommand() : base("GetMouse", true, "&Get Mouse Position", "&Keyboard/Mouse") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[0] { };
            }
        }

        public override string Description
        {
            get
            {
                return "Get the current mouse position.\n" +
                    "The result is a list, consisting of X and Y value.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            Point pos = context.InputEmulator.CursorPosition;
            return new MacroList(new MacroObject[] { pos.X, pos.Y });
        }
    }
}
