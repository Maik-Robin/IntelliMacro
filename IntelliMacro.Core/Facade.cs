using System.Collections.Generic;
using System.IO;
using System.Text;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    public static class Facade
    {
        public static void Init()
        {
            CommandRegistry.Instance.ScanPlugins();
        }

        public static Script ParseScript(string script)
        {
            Parser p = new Parser(new StringReader(script));
            return p.ParseScript();
        }

        public static MacroObject Evaluate(ScriptContext context, string expression)
        {
            Parser p = new Parser(new StringReader(expression));
            Expression expr = p.ParseExpression();
            return expr.Evaluate(context);
        }

        public static string Beautify(string input)
        {
            StringBuilder result = new StringBuilder();
            input = input.Replace("\r\n", "\n");
            while (input != null)
            {
                int pos = input.IndexOf("\n");
                string line;
                if (pos == -1)
                {
                    line = input;
                    input = null;
                }
                else
                {
                    line = input.Substring(0, pos);
                    input = input.Substring(pos + 1);
                }
                string prefix = "";
                while (line.StartsWith("\t") || line.StartsWith(" "))
                {
                    prefix += line.Substring(0, 1);
                    line = line.Substring(1);
                }
                while (true)
                {
                    try
                    {
                        List<ScriptCommand> commands = new List<ScriptCommand>();
                        Parser p = new Parser(new StringReader(line), true);
                        ScriptCommand currentCommand;
                        while ((currentCommand = p.ParseCommand()) != null)
                        {
                            commands.Add(currentCommand);
                        }
                        string comments = p.RecordedComments;
                        if (commands.Count > 0)
                        {
                            result.Append(prefix);
                            bool first = true;
                            foreach (ScriptCommand cmd in commands)
                            {
                                if (first)
                                    first = false;
                                else
                                    result.Append("; ");
                                result.Append(cmd.ToBeautifulString());
                            }
                        }
                        if (comments.Length > 0 && result.Length != 0 && result[result.Length - 1] != ' ' && result[result.Length - 1] != '\n')
                            result.Append(" ");
                        if (comments.Length > 0)
                            result.Append("#" + comments);
                        result.Append("\n");
                        break;
                    }
                    catch (ContinuationException)
                    {
                        pos = input.IndexOf("\n");
                        if (pos == -1)
                        {
                            line += "\n" + input;
                            input = null;
                        }
                        else
                        {
                            line += "\n" + input.Substring(0, pos);
                            input = input.Substring(pos + 1);
                        }
                    }
                    catch (ParserException)
                    {
                        result.Append(prefix + line + " ## Parse Error!" + "\n");
                        break;
                    }
                }
            }
            while (result[result.Length - 1] == '\n')
                result.Length--;
            return result.Replace("\n", "\r\n").ToString();
        }

        public static string ReIndent(string input)
        {
            string currentIndent = "";
            StringBuilder result = new StringBuilder();
            input = input.Replace("\r\n", "\n");
            while (input != null)
            {
                int pos = input.IndexOf("\n");
                string line;
                if (pos == -1)
                {
                    line = input;
                    input = null;
                }
                else
                {
                    line = input.Substring(0, pos);
                    input = input.Substring(pos + 1);
                }
                while (line.StartsWith("\t") || line.StartsWith(" "))
                {
                    line = line.Substring(1);
                }
                while (true)
                {
                    try
                    {
                        string indent = currentIndent;
                        Parser p = new Parser(new StringReader(line), true);
                        ScriptCommand cmd;
                        while ((cmd = p.ParseCommand()) != null)
                        {

                            if (cmd.Statement is BlockStatement)
                            {
                                BlockStatement b = (BlockStatement)cmd.Statement;
                                if (b.EndsBlock)
                                    currentIndent = currentIndent.Substring(4);
                                if (currentIndent.Length < indent.Length)
                                    indent = currentIndent;
                                if (b.StartsBlock)
                                    currentIndent += "    ";
                            }
                        }
                        result.Append(indent + line + "\n");
                        break;
                    }
                    catch (ContinuationException)
                    {
                        pos = input.IndexOf("\n");
                        if (pos == -1)
                        {
                            line += "\n" + input;
                            input = null;
                        }
                        else
                        {
                            line += "\n" + input.Substring(0, pos);
                            input = input.Substring(pos + 1);
                        }
                    }
                    catch (ParserException)
                    {
                        result.Append(currentIndent + line + "\n");
                        break;
                    }
                }
            }
            while (result[result.Length - 1] == '\n')
                result.Length--;
            return result.Replace("\n", "\r\n").ToString();
        }

        public static IList<IRecordingListener> RecordingListeners
        {
            get { return CommandRegistry.Instance.RecordingListeners; }
        }

        public static IEnumerable<ICommand> RegisteredCommands
        {
            get { return CommandRegistry.Instance.Commands; }
        }

        public static int RegisteredPluginCount { get { return CommandRegistry.Instance.PluginCount; } }

        public static int RegisteredCommandCount { get { return CommandRegistry.Instance.CommandCount; } }
    }
}
