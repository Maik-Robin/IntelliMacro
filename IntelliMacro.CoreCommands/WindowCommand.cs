using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class WindowCommand : AbstractCommand
    {
        internal WindowCommand() : base("Window", false, "Set active &window", "&Windows", false) { }
        int lastDelay = 1;

        public override string Description
        {
            get
            {
                return "Set the focus to the given window.\n\n" +
                    "The given window must be a toplevel window or a MDI child for this command to work.\n" +
                    "If that window does not exist, wait for 5 seconds if it appears. If it is still absent, an error message is shown.\n" +
                    "Lock execution to this window, i. e. an error is signaled if the current window changes while the macro is running.\n" +
                    "When called with an argument of -2, do not lock execution to any window any longer.\n" +
                    "When called with an argument of -1, set the focus to the locked window.\n" +
                    "When called with an argument of zero, lock execution to the current foreground window.";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Window path")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[0].IsNumber && parameters[0].Number == -2)
            {
                context.Window = null;
                SetDelay(1);
                return null;
            }
            WindowNode targetNode = FindWindow(parameters[0], context);
            if (targetNode == null)
            {
                lastDelay *= 2;
                if (lastDelay > 4000)
                {
                    lastDelay = 1;
                    if (MessageBox.Show("Cannot find the requested window.", "IntelliMacro.NET", MessageBoxButtons.RetryCancel, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button2, MB_SYSTEMMODAL) == DialogResult.Cancel)
                        throw new StopMacroException();
                }
                SetDelay(-lastDelay);
            }
            else
            {
                if (targetNode.Window.ParentSymmetric != null)
                {
                    if ((targetNode.Window.ExtendedStyle & WindowExStyleFlags.MDICHILD) != 0)
                    {
                        context.InputEmulator.ForegroundWindow = targetNode.Window;
                        SystemWindow w = targetNode.Window.ParentSymmetric;
                        while (w.ParentSymmetric != null) w = w.ParentSymmetric;
                        targetNode = new WindowNode(w);
                    }
                    else
                    {
                        throw new MacroErrorException("Cannot activate a child window");
                    }
                }
                context.Window = targetNode;
                context.InputEmulator.ForegroundWindow = context.Window.Window;
                lastDelay = 1;
                SetDelay(1);
            }
            return null;
        }

        internal static WindowNode FindWindow(MacroObject windowPath, MacroContext context)
        {
            WindowNode result;
            if (windowPath.IsNumber)
            {
                long hWnd = windowPath.Number;
                if (hWnd == -1 && context.Window == null) hWnd = 0;
                if (hWnd == 0)
                    result = new WindowNode(context.InputEmulator.ForegroundWindow);
                else if (hWnd == -1)
                    result = context.Window;
                else
                    result = new WindowNode(new SystemWindow(new IntPtr(hWnd)));
                try
                {
                    if (result.Window.ClassName.Length == 0) result = null;
                }
                catch
                {
                    result = null;
                }
            }
            else
            {
                result = PathParser.ParsePath(new WindowRoot(context.Window), windowPath.String, "Window");
            }
            return result;
        }
    }

    class FindWindowCommand : AbstractCommand
    {
        internal FindWindowCommand() : base("FindWindow", true, "&Find Window", "&Windows") { }

        public override string Description
        {
            get
            {
                return "Find a window matching the window path and return its handle.\n" +
                    "If no window matches, 0 is returned. If more than one window matches, an error is signaled.\n" +
                    "An argument of 0 finds the foreground window, -1 finds the window locked by the last Window command.\n";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Window path")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            WindowNode n = WindowCommand.FindWindow(parameters[0], context);
            long hWnd = n == null ? 0 : n.Window.HWnd.ToInt64();
            return hWnd;
        }
    }

    class FindWindowsCommand : AbstractCommand
    {
        internal FindWindowsCommand() : base("FindWindows", true, "Find Window&s", "&Windows") { }

        public override string Description
        {
            get
            {
                return "Find all windows matching the window paths and return their handles.\n\n" +
                    "This is the only window function that can include invisible windows, if needed.\n" +
                    "If baseHandle is set, paths are based on the given handle";
            }
        }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Window path"),
                    new ParameterDescription(true, "Include invisible windows"),
                    new ParameterDescription(true, "baseHandle")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            bool includeInvisible = parameters[1] != null && parameters[1].Number != 0;
            IPathRoot<WindowNode> root;
            if (parameters[2] == null)
                root = new WindowRoot(context.Window, includeInvisible);
            else
                root = new WindowNode(new SystemWindow((IntPtr)parameters[2].Number), includeInvisible);
            IList<WindowNode> windowNodes = PathParser.ParsePath(parameters[0].String, root);
            MacroObject[] result = new MacroObject[windowNodes.Count];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = windowNodes[i].Window.HWnd.ToInt64();
            }
            return new MacroList(result);
        }
    }

    class WindowFromPointCommand : AbstractCommand
    {
        internal WindowFromPointCommand() : base("WindowFromPoint", true, "Find window from poin&t", "&Windows") { }

        public override string Description
        {
            get
            {
                return "Find a window below a position on screen.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "X coordinate"),
                    new ParameterDescription(false, "Y coordinate"),
                    new ParameterDescription(true, "Screen number"),
                    new ParameterDescription(true, "Toplevel windows only"),
                    new ParameterDescription(true, "Include disabled windows")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int x = (int)parameters[0].Number, y = (int)parameters[1].Number;
            int screen = parameters[2] == null ? 0 : (int)parameters[2].Number;
            if (screen != 0)
                MouseCommand.ConvertCoordinatesForScreen(screen, ref x, ref y);
            bool toplevel = parameters[3] != null && parameters[3].Number != 0;
            bool disabled = parameters[4] != null && parameters[4].Number != 0;
            SystemWindow sw = SystemWindow.FromPointEx(x, y, toplevel, !disabled);
            if (sw == null) return MacroObject.ZERO;
            return sw.HWnd.ToInt64();
        }
    }

    class GetPositionCommand : AbstractCommand
    {
        bool rectangle;
        internal GetPositionCommand(string what)
            : base("Get" + what, true, "Get Window &" + what, "&Windows")
        {
            rectangle = (what == "Rectangle");
        }

        public override string Description
        {
            get
            {
                if (rectangle)
                {
                    return "Return the position of this window on the screen.\n\n" +
                        "The return value is a list: [x, y, width, height]";
                }
                else
                {
                    return "Return the position of this window relative to its parent.\n\n" +
                        "The return value is a list: [x, y, width, height]";
                }
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Window path")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            WindowNode n = WindowCommand.FindWindow(parameters[0], context);
            if (n == null) throw new MacroErrorException("Window not found");
            Rectangle pos = rectangle ? n.Window.Rectangle : n.Window.Position;
            MacroObject[] result = { pos.X, pos.Y, pos.Width, pos.Height };
            return new MacroList(result);
        }
    }

    class SetPositionCommand : AbstractCommand
    {

        internal SetPositionCommand() : base("SetPosition", false, "Set window p&osition", "&Windows") { }

        public override string Description
        {
            get
            {
                return "Set the position of a window.\n\n" +
                    "Be careful with this command, some windows do not like to be moved/resized.\n" +
                    "The window state can be 'n'ormal, 'min'imized or 'max'imized";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "Window path"),
                    new ParameterDescription(true, "X position"),
                    new ParameterDescription(true, "Y position"),
                    new ParameterDescription(true, "Width"),
                    new ParameterDescription(true, "Height"),
                    new ParameterDescription(true, "Window State"),
                    new ParameterDescription(true, "Topmost"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            WindowNode n = WindowCommand.FindWindow(parameters[0], context);
            if (n == null) throw new MacroErrorException("Window not found");
            Rectangle pos = n.Window.Position;
            if (parameters[1] != null) pos.X = (int)parameters[1].Number;
            if (parameters[2] != null) pos.Y = (int)parameters[2].Number;
            if (parameters[3] != null) pos.Width = (int)parameters[3].Number;
            if (parameters[4] != null) pos.Height = (int)parameters[4].Number;
            n.Window.Position = pos;
            if (parameters[5] != null)
            {
                switch (parameters[5].String.ToLowerInvariant())
                {
                    case "maximized":
                    case "max":
                        n.Window.WindowState = FormWindowState.Maximized;
                        break;
                    case "minimized":
                    case "min":
                        n.Window.WindowState = FormWindowState.Minimized;
                        break;
                    case "normal":
                    case "n":
                        n.Window.WindowState = FormWindowState.Normal;
                        break;
                    case "hide":
                        n.Window.VisibilityFlag = false;
                        break;
                    default:
                        throw new MacroErrorException("Unknown window state: " + parameters[5].String);
                }
            }
            if (parameters[6] != null)
            {
                n.Window.TopMost = parameters[6].Number != 0;
            }
            SetDelay(1);
            return null;
        }
    }

    class WindowInfoCommand : AbstractPathNodeInfoCommand<WindowNode>
    {
        internal WindowInfoCommand() : base("WindowInfo", "Window &Information", "&Windows") { }

        public override string Description
        {
            get
            {
                return "Obtain a parameter of a window and return it.\n\n" +
                    "See the window string builder for available parameters." + base.Description;
            }
        }

        protected override string PathName { get { return "Window path"; } }

        protected override WindowNode GetPathNode(MacroObject path, MacroContext context)
        {
            WindowNode n = WindowCommand.FindWindow(path, context);
            if (n == null) throw new MacroErrorException("Window not found");
            return n;
        }
    }
}
