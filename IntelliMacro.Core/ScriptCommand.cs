using System;
using System.Collections.Generic;
using IntelliMacro.Runtime;
using System.Text;
using System.IO;

namespace IntelliMacro.Core
{
    public class ScriptCommand
    {
        Script script = null;
        readonly int lineNumber;
        readonly string label;
        readonly Expression assignable;
        readonly Statement statement;

        internal ScriptCommand(int lineNumber, string label, Expression assignable, Statement statement)
        {
            if (assignable != null && !(statement is Expression)) throw new ArgumentException();
            if (assignable != null && !assignable.IsAssignable) throw new ArgumentException();
            this.lineNumber = lineNumber;
            this.label = label;
            this.assignable = assignable;
            this.statement = statement;
        }

        internal string Label { get { return label; } }
        internal Statement Statement { get { return statement; } }
        public int LineNumber { get { return lineNumber; } }

        internal void SetScript(Script script)
        {
            this.script = script;
        }

        // execute this script command and return the next one
        public ScriptCommand Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent)
        {
            waitEvent = null;
            if (assignable != null)
            {
                MacroObject result = ((Expression)statement).Evaluate(context);
                assignable.Assign(context, result);
                delay = 0;
            }
            else
            {
                IMacroEvent maybeWaitEvent;
                statement.Execute(context, out delay, out maybeWaitEvent);
                if (delay == AbstractCommand.EVENT_DELAY)
                {
                    waitEvent = maybeWaitEvent;
                    return this;
                }
                if (delay < 0)
                {
                    delay = -delay;
                    return this;
                }
            }
            if (statement is ControlFlowStatement)
            {
                return ((ControlFlowStatement)statement).GetNextCommand(context, script, this);
            }
            else
            {
                return script.GetCommandAfter(this);
            }
        }

        public ScriptCommand Skip(ScriptContext context)
        {
            if (statement is ControlFlowStatement)
            {
                ControlFlowStatement cfs = (ControlFlowStatement)statement;
                cfs.SkipCommand();
                return cfs.GetNextCommand(context, script, this);
            }
            else
            {
                return script.GetCommandAfter(this);
            }
        }

        public string ToBeautifulString()
        {
            string result = statement.ToBeautifulString();
            if (assignable != null)
            {
                result = assignable.ToBeautifulString() + " = " + result;
            }
            if (label != null)
            {
                result = label + ": " + result;
            }
            return result;
        }

        internal void WriteCSharp(TextWriter mainWriter, TextWriter subsWriter, ref string indent, IList<KeyValuePair<int, string>> jumpLabels, ref bool inSub, IDictionary<string, IList<string>> subVariables, IDictionary<string,SubStatement> subStatements)
        {
            TextWriter writer = inSub ? subsWriter : mainWriter;
            if (statement is BlockStatement && ((BlockStatement)statement).EndsBlock)
            {
                indent = indent.Substring(4);
                writer.WriteLine(indent + "    }");
                var oldJumpLabels = new List<KeyValuePair<int, string>>(jumpLabels);
                jumpLabels.Clear();
                bool endsSub = false;
                foreach (KeyValuePair<int, string> jumpLabel in oldJumpLabels)
                {
                    if (jumpLabel.Key == 1)
                    {
                        if (jumpLabel.Value == null)
                            endsSub = true;
                        else
                            writer.WriteLine(indent + jumpLabel.Value + ": ;");
                    }
                    else
                    {
                        jumpLabels.Add(new KeyValuePair<int, string>(jumpLabel.Key - 1, jumpLabel.Value));
                    }
                }
                if (endsSub)
                {
                    writer.WriteLine(indent + "}");
                    inSub = false;
                }
            }
            if (statement is SubStatement)
            {
                inSub = true;
                indent = indent.Substring(4);
                writer = subsWriter;
            }
            if (label != null)
            {
                writer.WriteLine(indent + "l" + IdentifierExpression.BuildCSharpIdentifier(label) + ":");
            }
            string overflow = null;
            if (assignable != null)
            {
                string value = ((Expression)statement).GetCSharpEvaluate();
                writer.WriteLine(indent + "    " + assignable.GetCSharpAssign(value) + ";");
            }
            else
            {
                if (statement is CallStatement)
                {
                    ((CallStatement)statement).targetStatement = subStatements[((CallStatement)statement).Name];
                }
                string line = statement.GetCSharpExecute();
                if (statement is ExitStatement)
                {
                    jumpLabels.Add(((ExitStatement)statement).GetCSharpJumpLabel());
                }
                if (line != null && line.IndexOf('\0') != -1)
                {
                    overflow = line.Substring(line.IndexOf('\0') + 1);
                    line = line.Substring(0, line.IndexOf('\0'));
                }
                if (line != null)
                    writer.WriteLine(indent + "    " + line);
                else if (label != null)
                    writer.WriteLine(indent + "    ;");
            }
            if (statement is BlockStatement && ((BlockStatement)statement).StartsBlock)
            {
                if (statement is SubStatement)
                {
                    jumpLabels.Add(new KeyValuePair<int, string>(0, null));
                    indent = indent + "    ";
                    foreach (string v in subVariables[((SubStatement)statement).Name])
                    {
                        writer.WriteLine(indent + "    MacroObject " + v + @" = """";");
                    }
                }
                writer.WriteLine(indent + "    {");
                indent = indent + "    ";
                var oldJumpLabels = new List<KeyValuePair<int, string>>(jumpLabels);
                jumpLabels.Clear();
                foreach (KeyValuePair<int, string> jumpLabel in oldJumpLabels)
                {
                    jumpLabels.Add(new KeyValuePair<int, string>(jumpLabel.Key + 1, jumpLabel.Value));
                }
            }
            if (overflow != null)
                writer.WriteLine(indent + "    " + overflow);
        }

        internal void AddDeclaringTypes(List<Type> types)
        {
            statement.AddDeclaringTypes(types);
        }

        internal void AddVariables(List<string> variables)
        {
            if (assignable != null) assignable.AddVariables(variables);
            statement.AddVariables(variables);
        }
    }
}
