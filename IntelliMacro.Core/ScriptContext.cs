using System;
using System.Collections.Generic;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    public class ScriptContext
    {
        Dictionary<string, MacroObject> variables = new Dictionary<string, MacroObject>();

        public MacroContext MacroContext { get; private set; }
        internal BlockStateStack BlockStateStack { get; private set; }
        internal int RepeatCount { get; set; }

        public ScriptContext(MacroContext context)
        {
            BlockStateStack = new BlockStateStack();
            MacroContext = context;
            RepeatCount = 0;
        }

        /// <summary>
        /// Get a macro variable's value, or <see cref="MacroObject.EMPTY"/> if the
        /// variable is uninitialized
        /// </summary>
        public MacroObject GetVariable(string varname)
        {
            varname = varname.ToLowerInvariant();
            if (variables.ContainsKey(varname))
                return variables[varname];
            else
                return MacroObject.EMPTY;
        }

        /// <summary>
        /// Clear all local variables in this object and return a dictionary that
        /// contains all of them.
        /// </summary>
        internal Dictionary<string, MacroObject> ExtractLocalVariables()
        {
            Dictionary<string, MacroObject> result = new Dictionary<string, MacroObject>();
            foreach (KeyValuePair<string, MacroObject> var in variables)
            {
                if (!var.Key.StartsWith("g_"))
                    result.Add(var.Key, var.Value);
            }
            foreach (string var in result.Keys)
            {
                variables.Remove(var);
            }
            return result;
        }

        /// <summary>
        /// Set a macro variable's value.
        /// </summary>
        /// <returns>the value</returns>
        public void SetVariable(string varname, MacroObject value)
        {
            if (value == null)
                throw new ArgumentException("Cannot assign null to a variable");
            variables[varname.ToLowerInvariant()] = value;
        }
    }
}
