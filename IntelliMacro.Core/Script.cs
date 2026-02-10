using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace IntelliMacro.Core
{
    public class Script
    {
        private IList<ScriptCommand> commands = new List<ScriptCommand>();

        internal Script() { }

        internal void AddCommand(ScriptCommand cmd)
        {
            cmd.SetScript(this);
            commands.Add(cmd);
        }

        public IList<ScriptCommand> Commands
        {
            get { return commands; }
        }

        internal ScriptCommand GetCommandAtLine(int line)
        {
            foreach (ScriptCommand cmd in commands)
            {
                if (cmd.LineNumber >= line) return cmd;
            }
            return null;
        }

        internal ScriptCommand GetCommandAfter(ScriptCommand previousCommand)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (commands[i] == previousCommand)
                {
                    if (i == commands.Count - 1) return null;
                    return commands[i + 1];
                }
            }
            throw new Exception("Command not found");
        }

        internal void TestBlockCommands()
        {
            int depth = 0;
            foreach (ScriptCommand cmd in commands)
            {
                if (cmd.Statement is BlockStatement)
                {
                    depth -= ((BlockStatement)cmd.Statement).EndsBlock ? 1 : 0;
                    if (depth < 0) throw new ParserException(cmd.LineNumber, 0, "Block closed that was not open");
                    depth += ((BlockStatement)cmd.Statement).StartsBlock ? 1 : 0;
                }
            }
            if (depth != 0) throw new ParserException(commands[commands.Count - 1].LineNumber + 1, 0, "Unclosed block");
        }

        private IList<Type> DeclaringTypes
        {
            get
            {
                List<Type> types = new List<Type>();
                foreach (ScriptCommand cmd in commands)
                {
                    cmd.AddDeclaringTypes(types);
                }
                types.Sort((a, b) => a.FullName.CompareTo(b.FullName));
                for (int i = 1; i < types.Count; i++)
                {
                    while (i < types.Count && types[i] == types[i - 1])
                    {
                        types.RemoveAt(i);
                    }
                }
                return types;
            }
        }

        internal bool ContainsLabel(string label)
        {
            foreach (ScriptCommand cmd in commands)
            {
                if (cmd.Label == label)
                    return true;
            }
            return false;
        }

        private IDictionary<string, IList<string>> VariableNames
        {
            get
            {
                IDictionary<string, IList<string>> result = new Dictionary<string, IList<string>>();
                List<string> globals = new List<string>();
                result.Add("*", globals);
                List<string> locals = new List<string>();
                result.Add("", locals);
                List<string> current = locals;
                int subdepth = -1;
                IList<string> subParams = null;
                foreach (ScriptCommand cmd in commands)
                {
                    if (cmd.Statement is SubStatement)
                    {
                        if (subdepth != -1) throw new Exception("Cannot have a Sub inside a Sub");
                        string subname = ((SubStatement)cmd.Statement).Name;
                        subParams = ((SubStatement)cmd.Statement).Parameters;
                        current = new List<string>();
                        result.Add(subname, current);
                        subdepth = 0;
                    }
                    if (subdepth != -1)
                    {
                        if (cmd.Statement is BlockStatement && ((BlockStatement)cmd.Statement).StartsBlock)
                        {
                            subdepth++;
                        }
                        if (cmd.Statement is BlockStatement && ((BlockStatement)cmd.Statement).EndsBlock)
                        {
                            subdepth--;
                            if (subdepth == 0)
                            {
                                SortAndDeduplicate(current, "gv", globals);
                                foreach (string rawParam in subParams)
                                {
                                    string param = "v" + IdentifierExpression.BuildCSharpIdentifier(rawParam.StartsWith("&") ? rawParam.Substring(1) : rawParam);
                                    if (current.Contains(param))
                                        current.Remove(param);
                                }
                                current = locals;
                                subdepth = -1;
                                subParams = null;
                            }
                        }
                    }
                    cmd.AddVariables(current);
                }
                if (subdepth != -1) throw new Exception("Sub did not end");
                SortAndDeduplicate(locals, "gv", globals);
                SortAndDeduplicate(globals, "v", null);
                return result;
            }
        }

        private void SortAndDeduplicate(List<string> list, string overflowPrefix, IList<string> overflowList)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartsWith(overflowPrefix))
                {
                    overflowList.Add(list[i]);
                    list.RemoveAt(i);
                    i--;
                }
            }
            list.Sort();
            for (int i = 1; i < list.Count; i++)
            {
                while (i < list.Count && list[i] == list[i - 1])
                {
                    list.RemoveAt(i);
                }
            }
        }

        public void WriteCSharpProgram(string filename)
        {
            TextWriter writer = new StreamWriter(new FileStream(filename, FileMode.Create, FileAccess.Write), Encoding.UTF8);
            IList<Type> declaringTypes = DeclaringTypes;
            writer.WriteLine("// Exported by IntelliMacro " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            writer.WriteLine("using System;");
            writer.WriteLine("using IntelliMacro.Runtime;");
            foreach (Type t in declaringTypes)
            {
                writer.WriteLine("using " + t.Namespace + ";");
            }
            string[] constantLines = 
            {
                "",
                "namespace ExportedMacro",
                "{",
                "    public static class "+Path.GetFileNameWithoutExtension(filename),
                "    {",
                "        [STAThread]",
                "        static void Main(string[] args)",
                "        {",
                "            Run();",
                "        }",
                ""
            };
            foreach (string line in constantLines) { writer.WriteLine(line); }
            IDictionary<string, IList<string>> vars = VariableNames;
            foreach (string v in vars["*"])
            {
                writer.WriteLine("        private static MacroObject " + v + @" = """";");
            }
            writer.WriteLine("");
            writer.WriteLine("        static void Run()");
            writer.WriteLine("        {");
            writer.WriteLine("            MacroCommandRunner r = new MacroCommandRunner();");
            foreach (string v in vars[""])
            {
                writer.WriteLine("            MacroObject " + v + @" = """";");
            }
            writer.WriteLine("            int repeatCount = 0;");
            writer.WriteLine("        repeatLabel:");
            StringWriter subs = new StringWriter();
            string indent = "        ";
            var jumpLabels = new List<KeyValuePair<int, string>>();
            bool inSub = false;
            var subStatements = new Dictionary<string, SubStatement>();
            foreach (ScriptCommand cmd in commands)
            {
                if (cmd.Statement is SubStatement)
                {
                    SubStatement ss = (SubStatement)cmd.Statement;
                    subStatements[ss.Name] = ss;
                }
            }
            foreach (ScriptCommand cmd in commands)
            {
                cmd.WriteCSharp(writer, subs, ref indent, jumpLabels, ref inSub, vars, subStatements);
            }
            foreach (KeyValuePair<int, string> jumpLabel in jumpLabels)
            {
                writer.WriteLine(indent + jumpLabel.Value + ": ;");
            }
            writer.WriteLine("            ;");
            writer.WriteLine("        }");
            writer.Write(subs.ToString());
            writer.WriteLine("    }");
            writer.WriteLine("}");
            writer.Close();
        }
    }
}
