using System;
using System.Collections.Generic;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;

namespace IntelliMacro.CoreCommands
{
    /// <summary>
    /// This class is a base for "Info" commands based on a path node.
    /// </summary>
    abstract class AbstractPathNodeInfoCommand<T> : AbstractCommand where T : class, IPathNode<T>
    {
        public AbstractPathNodeInfoCommand(string name, string displayName, string displayCategory) : base(name, true, displayName, displayCategory) { }

        protected virtual string PathName { get { return "Path"; } }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, PathName),
                    new ParameterDescription(false, "Parameter name")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "The value -1 instead of a parameter name will return all parameters.";
            }
        }

        protected abstract T GetPathNode(MacroObject path, MacroContext context);

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            T n = GetPathNode(parameters[0], context);
            if (parameters[1].IsNumber && parameters[1].Number == -1)
            {
                List<MacroObject> names = new List<MacroObject>();
                List<MacroObject> values = new List<MacroObject>();
                foreach (string name in PathParser.GetAllParameterNames(n))
                {
                    names.Add(name);
                    values.Add(n.GetParameter(name));
                }
                return new MacroList(new MacroObject[] { n.NodeName, new MacroList(names), new MacroList(values) });
            }
            String param = parameters[1].String;
            if (param == "") return n.NodeName;
            return n.GetParameter(param);
        }
    }
}
