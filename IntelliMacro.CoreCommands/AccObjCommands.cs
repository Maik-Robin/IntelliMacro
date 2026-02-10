using System.Collections.Generic;
using System.Runtime.InteropServices;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi.Accessibility;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class FindAccObjCommand : AbstractCommand
    {
        internal FindAccObjCommand() : base("FindAccObj", true, "&Find object", "Screen&reader objects") { }

        public override string Description
        {
            get
            {
                return "Find an accessible object (screenreader object) matching a path.\n\n" +
                    "The base object can either be a window string or another accessible object.\n" +
                    "If the base object is -2, the text cursor (caret) is returned, if it is -3, the mouse cursor (pointer) is returned.\n" +
                    "Path can be empty (in which case the object itself is returned) or an accobj path.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Base Object"),
                    new ParameterDescription(true, "Path"),
                    new ParameterDescription(true, "Base object relation")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            AccessibleObjectID relation = parameters[2] == null ? AccessibleObjectID.OBJID_WINDOW : (AccessibleObjectID)parameters[2].Number;
            string path = parameters[1] == null ? "" : parameters[1].String;
            AccessibleObjectNode node = new AccessibleObjectNode(FindNode(parameters[0], relation, context), false);
            SystemAccessibleObject sao = node.AccessibleObject;
            if (path != "")
                sao = PathParser.ParsePath(node, path, "accessible object").AccessibleObject;
            return new MacroWrappedObject(sao);
        }

        internal static SystemAccessibleObject FindNode(MacroObject baseObject, AccessibleObjectID relation, MacroContext context)
        {
            if (MacroWrappedObject.Unwrap(baseObject) is SystemAccessibleObject)
            {
                return (SystemAccessibleObject)((MacroWrappedObject)baseObject).Wrapped;
            }
            else if (baseObject.IsNumber)
            {
                if (baseObject.Number == -2)
                {
                    return SystemAccessibleObject.Caret;
                }
                else if (baseObject.Number == -3)
                {
                    return SystemAccessibleObject.MouseCursor;
                }
            }
            return SystemAccessibleObject.FromWindow(WindowCommand.FindWindow(baseObject, context).Window, relation);
        }
    }

    class FindAccObjsCommand : AbstractCommand
    {
        internal FindAccObjsCommand() : base("FindAccObjs", true, "Find &objects", "Screen&reader objects") { }

        public override string Description
        {
            get
            {
                return "Find all accessible object (screenreader object) matching a path.\n\n" +
                    "The base object can either be a window string or another accessible object.\n" +
                    "If the base object is -2, the text cursor (caret) is returned, if it is -3, the mouse cursor (pointer) is returned.\n" +
                    "Path can be empty (in which case the object itself is returned) or an accobj path.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Base Object"),
                    new ParameterDescription(true, "Path"),
                    new ParameterDescription(true, "Base object relation")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            AccessibleObjectID relation = parameters[2] == null ? AccessibleObjectID.OBJID_WINDOW : (AccessibleObjectID)parameters[2].Number;
            string path = parameters[1] == null ? "" : parameters[1].String;

            AccessibleObjectNode node = new AccessibleObjectNode(FindAccObjCommand.FindNode(parameters[0], relation, context), false);
            IList<AccessibleObjectNode> nodes = new AccessibleObjectNode[] { node };
            if (path != "")
            {
                nodes = PathParser.ParsePath(path, node);
            }
            List<MacroObject> result = new List<MacroObject>();
            foreach (AccessibleObjectNode n in nodes)
            {
                result.Add(new MacroWrappedObject(n.AccessibleObject));
            }
            return new MacroList(result);
        }
    }


    class AccObjFromPointCommand : AbstractCommand
    {
        internal AccObjFromPointCommand() : base("AccObjFromPoint", true, "Find from &point", "Screen&reader objects") { }

        public override string Description
        {
            get
            {
                return "Find the accessible object below a point on screen.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "X coordinate"),
                    new ParameterDescription(false, "Y coordinate"),
                    new ParameterDescription(true, "Screen number")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int x = (int)parameters[0].Number, y = (int)parameters[1].Number;
            int screen = parameters[2] == null ? 0 : (int)parameters[2].Number;
            if (screen != 0)
                MouseCommand.ConvertCoordinatesForScreen(screen, ref x, ref y);
            return new MacroWrappedObject(SystemAccessibleObject.FromPoint(x, y));
        }
    }

    class AccObjInfoCommand : AbstractPathNodeInfoCommand<AccessibleObjectNode>
    {
        internal AccObjInfoCommand() : base("AccObjInfo", "Get &Information", "Screen&reader objects") { }

        public override string Description
        {
            get
            {
                return "Obtain a parameter of a screenreader object (accessible object) and return it.\n\n" +
                    "See the window string builder for available parameters. " + base.Description;
            }
        }

        protected override string PathName { get { return "Accessible Object Path"; } }

        protected override AccessibleObjectNode GetPathNode(MacroObject path, MacroContext context)
        {
            SystemAccessibleObject sao = MacroWrappedObject.Unwrap(path) as SystemAccessibleObject;
            if (sao == null) throw new MacroErrorException("No accessible object given.");
            return new AccessibleObjectNode(sao, false);
        }
    }

    class InvokeAccObjCommand : AbstractCommand
    {
        internal InvokeAccObjCommand() : base("InvokeAccObj", false, "In&voke", "Screen&reader objects") { }

        public override string Description
        {
            get
            {
                return "Invoke the action of a screenreader object.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Accessible object")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            SystemAccessibleObject sao = MacroWrappedObject.Unwrap(parameters[0]) as SystemAccessibleObject;
            if (sao == null) throw new MacroErrorException("No accessible object given.");
            sao.DoDefaultAction();
            SetDelay(1);
            return null;
        }
    }

    class MenuCommand : AbstractCommand
    {
        internal MenuCommand() : base("Menu", false, "Invoke M&enu command", "&Keyboard/Mouse") { }

        public override string Description
        {
            get { return "Invoke a menu command in the foreground window."; }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Menu command")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            string path = parameters[0].String;
            SystemWindow fg = context.InputEmulator.ForegroundWindow;
            SystemAccessibleObject menu = SystemAccessibleObject.FromWindow(fg, AccessibleObjectID.OBJID_MENU);
            if (path.StartsWith("|")) // System menu
            {
                path = path.Substring(1);
                menu = SystemAccessibleObject.FromWindow(fg, AccessibleObjectID.OBJID_SYSMENU);
            }
            AccessibleObjectNode item = PathParser.ParsePath(new AccessibleObjectNode(menu, true), path, "menu item");
            if (item == null) throw new MacroErrorException("Menu item not found");
            try
            {
                item.AccessibleObject.DoDefaultAction();
            }
            catch (COMException ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            SetDelay(1);
            return null;
        }
    }
}
