using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using ManagedWinapi;

namespace IntelliMacro.Core
{
    abstract class Statement
    {
        internal abstract void Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent);
        internal abstract string GetCSharpExecute();
        internal abstract string ToBeautifulString();

        internal virtual void AddDeclaringTypes(IList<Type> types) { }
        internal virtual void AddVariables(IList<string> variables) { }
    }

    class VoidStatement : Statement
    {
        internal override void Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent)
        {
            delay = 0;
            waitEvent = null;
        }
        internal override string GetCSharpExecute()
        {
            return null;
        }
        internal override string ToBeautifulString()
        {
            return "";
        }
    }

    class CommandStatement : CommandExpression
    {
        public CommandStatement(ICommand command, Expression[] parameters) : base(command, parameters, false) { }

        internal override void Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent)
        {
            base.Execute(context, out delay, out waitEvent);
            delay = command.Delay;
            waitEvent = delay == AbstractCommand.EVENT_DELAY ? command.WaitEvent : null;
        }

        internal override string GetCSharpExecute()
        {
            StringBuilder result = new StringBuilder("r.Invoke(");
            result.Append(CommandRegistry.Instance.GetDeclaringType(command.Name).Name);
            result.Append(".").Append(command.Name);
            foreach (Expression param in parameters)
            {
                result.Append(", ").Append(param == null ? "(MacroObject)null" : param.GetCSharpEvaluate());
            }
            result.Append(");");
            return result.ToString();
        }
    }

    abstract class ControlFlowStatement : Statement
    {
        internal abstract void SkipCommand();
        internal virtual ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            return script.GetCommandAfter(scriptCommand);
        }

        internal sealed override void Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent)
        {
            Execute(context);
            delay = 0; // to slow down infinite loops
            waitEvent = null;
        }

        internal abstract void Execute(ScriptContext context);
    }

    class GotoStatement : ControlFlowStatement
    {
        string label;
        readonly Expression condition, dynamicLabel;
        bool lastExpressionValue = true;

        public GotoStatement(string label, Expression condition, Expression dynamicLabel)
        {
            this.label = label;
            this.condition = condition;
            this.dynamicLabel = dynamicLabel;
        }

        internal override void Execute(ScriptContext context)
        {
            if (dynamicLabel != null)
                label = dynamicLabel.Evaluate(context).String.ToLowerInvariant();
            if (condition != null)
                lastExpressionValue = condition.Evaluate(context).Number != 0;
        }

        internal override string GetCSharpExecute()
        {
            if (dynamicLabel != null)
                return "#error Dynamic Goto does not work in C#";
            string result = "goto l" + IdentifierExpression.BuildCSharpIdentifier(label) + ";";
            if (condition != null)
                result = "if (" + condition.GetCSharpEvaluate() + ".Number != 0) " + result;
            return result;
        }

        internal override void SkipCommand()
        {
            if (condition != null)
            {
                lastExpressionValue = MessageBox.Show("Follow the jump?", "IntelliMacro.NET", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
            }
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            if (lastExpressionValue)
            {
                ScriptCommand targetCommand = null;
                foreach (ScriptCommand cmd in script.Commands)
                {
                    if (cmd.Label == label)
                        targetCommand = cmd;
                }
                if (targetCommand == null) throw new MacroErrorException("Label not found: " + label);
                int from = scriptCommand.LineNumber, to = targetCommand.LineNumber;
                if (from > to) { int tmp = from; from = to; to = tmp; }
                foreach (ScriptCommand cmd in script.Commands)
                {
                    if (cmd.LineNumber > from && cmd.LineNumber < to)
                    {
                        if (cmd.Statement is BlockStatement)
                        {
                            throw new MacroErrorException("Goto beyond a block command is not supported");
                        }
                    }
                }
                return targetCommand;
            }
            else
            {
                return base.GetNextCommand(context, script, scriptCommand);
            }
        }

        internal override string ToBeautifulString()
        {
            return "GoTo " + label.ToLowerInvariant() +
                (condition == null ? "" : " If " + condition.ToBeautifulString());
        }

        internal override void AddVariables(IList<string> variables)
        {
            if (condition != null)
                condition.AddVariables(variables);
        }
    }

    class CallStatement : ControlFlowStatement
    {
        readonly string subname;
        readonly IList<Expression> parameters;
        CallSubContext lastContext = null;

        public CallStatement(string subname, IList<Expression> parameters)
        {
            this.subname = subname.ToLowerInvariant();
            this.parameters = parameters;
        }

        internal string Name { get { return subname; } }

        internal override void Execute(ScriptContext context)
        {
            SubStatement sub = MacroWrappedObject.Unwrap(context.GetVariable("g_*sub*" + subname)) as SubStatement;
            if (sub == null)
                throw new MacroErrorException("Procedure '" + subname + "' not found");

            MacroObject[] evaluatedParameters = new MacroObject[parameters.Count];
            for (int i = 0; i < evaluatedParameters.Length; i++)
            {
                evaluatedParameters[i] = parameters[i].Evaluate(context);
            }

            lastContext = new CallSubContext(sub, context.ExtractLocalVariables());

            if (evaluatedParameters.Length != sub.Parameters.Count)
                throw new MacroErrorException("Invalid number of parameters");
            for (int i = 0; i < evaluatedParameters.Length; i++)
            {
                string param = sub.Parameters[i];
                if (param.StartsWith("&"))
                {
                    param = param.Substring(1);
                    if (!parameters[i].IsAssignable)
                        throw new MacroErrorException("Cannot pass '" + parameters[i].ToBeautifulString() + "' as reference");
                    lastContext.RefParameters.Add(param, parameters[i]);
                }
                context.SetVariable(param, evaluatedParameters[i]);
            }
        }

        internal override void SkipCommand()
        {
            lastContext = null;
        }

        internal SubStatement targetStatement;
        static int refCounter = 0;

        internal override string GetCSharpExecute()
        {
            if (parameters.Count != targetStatement.Parameters.Count)
                throw new MacroErrorException("Invalid number of parameters");
            string resultPrefix = "";
            string result = "sub" + IdentifierExpression.BuildCSharpIdentifier(subname) + "(r";
            string resultSuffix = "";
            for (int i = 0; i < parameters.Count; i++)
            {
                if (targetStatement.Parameters[i].StartsWith("&"))
                {
                    refCounter++;
                    resultPrefix += "MacroObject ref_" + refCounter + " = " + parameters[i].GetCSharpEvaluate() + "; ";
                    result += ", ref ref_" + refCounter;
                    resultSuffix += "; " + parameters[i].GetCSharpAssign("ref_" + refCounter);
                }
                else
                {
                    result += ", " + parameters[i].GetCSharpEvaluate();
                }
            }
            return resultPrefix + result + ")" + resultSuffix + ";";
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            if (lastContext == null)
                return base.GetNextCommand(context, script, scriptCommand);

            SubStatement sub = lastContext.SubStatement;
            ScriptCommand targetCommand = null;
            foreach (ScriptCommand cmd in script.Commands)
            {
                if (cmd.Statement == sub)
                    targetCommand = cmd;
            }

            // this should not happen, as we already found the SubStatement object before.
            if (targetCommand == null) throw new Exception("Procedure entry point not found");

            context.BlockStateStack.Push(new CallSubState(targetCommand, lastContext, script.GetCommandAfter(scriptCommand)));
            return script.GetCommandAfter(targetCommand);
        }

        internal override string ToBeautifulString()
        {
            StringBuilder sb = new StringBuilder("Call " + subname + "(");
            bool first = true;
            foreach (Expression param in parameters)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(param.ToBeautifulString());
            }
            return sb.Append(")").ToString();
        }

        internal override void AddVariables(IList<string> variables)
        {
            foreach (Expression parameter in parameters)
                parameter.AddVariables(variables);
        }
    }
    abstract class BlockStatement : ControlFlowStatement
    {
        internal abstract bool StartsBlock { get; }
        internal abstract bool EndsBlock { get; }

        internal ScriptCommand GetMatchingEndCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            if (!StartsBlock) throw new Exception("No block start");
            int depth = StartsBlock ? 1 : 0;
            ScriptCommand cmd = script.GetCommandAfter(scriptCommand);
            while (cmd != null)
            {
                if (cmd.Statement is BlockStatement)
                {
                    depth -= ((BlockStatement)cmd.Statement).EndsBlock ? 1 : 0;
                    if (depth == 0)
                    {
                        return cmd;
                    }
                    depth += ((BlockStatement)cmd.Statement).StartsBlock ? 1 : 0;
                }
                cmd = script.GetCommandAfter(cmd);
            }
            throw new Exception("Block not closed");
        }
    }

    class SubStatement : BlockStatement
    {
        protected readonly string subname;
        protected readonly IList<string> parameters;

        internal override bool StartsBlock { get { return true; } }
        internal override bool EndsBlock { get { return false; } }

        internal SubStatement(string subname, IList<string> parameters)
        {
            this.subname = subname.ToLowerInvariant();
            this.parameters = parameters;
        }

        internal string Name { get { return subname; } }

        internal IList<string> Parameters { get { return parameters; } }

        internal override void SkipCommand() { }

        internal override void Execute(ScriptContext context)
        {
            context.SetVariable("g_*sub*" + subname, new MacroWrappedObject(this));
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            // when executing a sub definition, we treat it like an "if false".
            context.BlockStateStack.Push(new IfState(scriptCommand, false));
            return GetMatchingEndCommand(context, script, scriptCommand);
        }

        internal override string ToBeautifulString()
        {
            StringBuilder sb = new StringBuilder("Sub " + subname + "(");
            bool first = true;
            foreach (string param in parameters)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(param);
            }
            return sb.Append(")").ToString();
        }

        internal override string GetCSharpExecute()
        {
            StringBuilder p = new StringBuilder();
            foreach (string param in parameters)
            {
                p.Append(", ");
                if (param.StartsWith("&"))
                    p.Append("ref MacroObject v" + IdentifierExpression.BuildCSharpIdentifier(param.Substring(1)));
                else
                    p.Append("MacroObject v" + IdentifierExpression.BuildCSharpIdentifier(param));
            }

            return "private static void sub" + IdentifierExpression.BuildCSharpIdentifier(subname) + "(MacroCommandRunner r" + p.ToString() + ") {";
        }
    }

    abstract class ConditionalStatement : BlockStatement
    {
        protected readonly Expression condition;
        protected bool lastExpressionValue = false;

        internal override bool StartsBlock { get { return true; } }

        protected ConditionalStatement(Expression condition) { this.condition = condition; }

        internal override void SkipCommand()
        {
            lastExpressionValue = MessageBox.Show("Evaluate this block?", "IntelliMacro.NET", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        internal override void Execute(ScriptContext context)
        {
            lastExpressionValue = condition.Evaluate(context).Number != 0;
        }

        internal override void AddVariables(IList<string> variables)
        {
            condition.AddVariables(variables);
        }
    }

    class IfStatement : ConditionalStatement
    {
        internal IfStatement(Expression condition) : base(condition) { }
        internal override bool EndsBlock { get { return false; } }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            context.BlockStateStack.Push(new IfState(scriptCommand, lastExpressionValue));
            if (lastExpressionValue)
                return base.GetNextCommand(context, script, scriptCommand);
            else
                return GetMatchingEndCommand(context, script, scriptCommand);
        }

        internal override string ToBeautifulString()
        {
            return "If " + condition.ToBeautifulString();
        }

        internal override string GetCSharpExecute()
        {
            return ("if (" + condition.GetCSharpEvaluate() + ".Number != 0)");
        }
    }

    class ElseIfStatement : ConditionalStatement
    {
        internal ElseIfStatement(Expression condition) : base(condition) { }
        internal override bool EndsBlock { get { return true; } }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            IfState state = context.BlockStateStack.Top as IfState;
            if (state == null) throw new MacroErrorException("Unmatched else statement.");
            if (state.wasEntered) lastExpressionValue = false;
            state.wasEntered |= lastExpressionValue;
            if (lastExpressionValue)
                return base.GetNextCommand(context, script, scriptCommand);
            else
                return GetMatchingEndCommand(context, script, scriptCommand);
        }

        internal override string ToBeautifulString()
        {
            if (condition.ToBeautifulString() == "1")
                return "Else";
            return "ElseIf " + condition.ToBeautifulString();
        }

        internal override string GetCSharpExecute()
        {
            if (condition.ToBeautifulString() == "1")
                return "else";
            return ("else if (" + condition.GetCSharpEvaluate() + ".Number != 0)");
        }
    }

    class WhileStatement : ConditionalStatement
    {
        internal WhileStatement(Expression condition) : base(condition) { }
        internal override bool EndsBlock { get { return false; } }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            context.BlockStateStack.Push(GetBlockState(scriptCommand));
            if (lastExpressionValue)
                return base.GetNextCommand(context, script, scriptCommand);
            else
                return GetMatchingEndCommand(context, script, scriptCommand);
        }

        protected virtual WhileState GetBlockState(ScriptCommand scriptCommand)
        {
            return new WhileState(scriptCommand, lastExpressionValue);
        }

        internal override string ToBeautifulString()
        {
            return "While " + condition.ToBeautifulString();
        }

        internal override string GetCSharpExecute()
        {
            return ("while (" + condition.GetCSharpEvaluate() + ".Number != 0)");
        }
    }

    class ForStatement : WhileStatement
    {
        Expression assignable, from, to;
        internal MacroObject remaining = null;

        internal ForStatement(Expression assignable, Expression iterable)
            : base(null)
        {
            this.assignable = assignable;
            this.from = iterable;
            this.to = null;
        }

        internal ForStatement(Expression assignable, Expression from, Expression to)
            : base(null)
        {
            this.assignable = assignable;
            this.from = from;
            this.to = to;
        }

        internal override void SkipCommand()
        {
            base.SkipCommand();
            if (!lastExpressionValue) remaining = null;
        }

        internal override void Execute(ScriptContext context)
        {
            if (to == null) // foreach
            {
                if (remaining == null) // first call
                {
                    remaining = from.Evaluate(context);
                }
                if (remaining.Length == 0)
                {
                    remaining = null;
                    lastExpressionValue = false;
                }
                else
                {
                    lastExpressionValue = true;
                    assignable.Assign(context, remaining[MacroObject.ONE]);
                    remaining = remaining.SetSlice(MacroObject.ONE, MacroObject.ONE, MacroObject.EMPTY);
                }
            }
            else // for
            {
                if (remaining == null) // first call
                {
                    remaining = from.Evaluate(context).Number;
                    assignable.Assign(context, remaining);
                    lastExpressionValue = true;
                }
                else
                {
                    long current = remaining.Number;
                    long final = to.Evaluate(context).Number;
                    if (current == final)
                    {
                        remaining = null;
                        lastExpressionValue = false;
                    }
                    else
                    {
                        current = current + (final > current ? 1 : -1);
                        remaining = current;
                        assignable.Assign(context, remaining);
                        lastExpressionValue = true;
                    }
                }
            }
        }

        protected override WhileState GetBlockState(ScriptCommand scriptCommand)
        {
            WhileState result;
            if (remaining != null)
            {
                result = new ForState(scriptCommand, lastExpressionValue, remaining);
                remaining = null;
            }
            else
            {
                result = base.GetBlockState(scriptCommand);
            }
            return result;
        }

        static int forCounter = 0;

        internal override string GetCSharpExecute()
        {
            forCounter++;
            if (to == null) // foreach
            {
                return "foreach (MacroObject f_" + forCounter + " in " + from.GetCSharpEvaluate() + ")\0" +
                    assignable.GetCSharpAssign("f_" + forCounter) + ";";
            }
            else // for
            {
                return "for(ForLoopHandler f_" + forCounter + " = new ForLoopHandler(" + from.GetCSharpEvaluate() + ".Number); f_" + forCounter + ".HasNotExceeded(" + to.GetCSharpEvaluate() + ".Number); f_" + forCounter + ".Increment())\0" +
                    assignable.GetCSharpAssign("f_" + forCounter + ".Value") + ";";
            }
        }

        internal override string ToBeautifulString()
        {
            if (to == null)
            {
                return "For " + assignable.ToBeautifulString() + " : " + from.ToBeautifulString();
            }
            else
            {
                return "For " + assignable.ToBeautifulString() + " = " + from.ToBeautifulString() + " .. " + to.ToBeautifulString();
            }
        }

        internal override void AddVariables(IList<string> variables)
        {
            assignable.AddVariables(variables);
            from.AddVariables(variables);
            if (to != null)
                to.AddVariables(variables);
        }
    }

    class ExitStatement : ControlFlowStatement
    {
        readonly int depth;

        internal ExitStatement(int depth) { this.depth = depth; }

        internal override void SkipCommand() { }
        internal override void Execute(ScriptContext context) { }

        static int exitCounter = 0;

        internal override string GetCSharpExecute()
        {
            exitCounter++;
            return "goto exit_" + exitCounter + ";";
        }

        internal new KeyValuePair<int, string> GetCSharpJumpLabel()
        {
            return new KeyValuePair<int, string>(depth == 0 ? int.MaxValue : depth, "exit_" + exitCounter);
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            BlockState state = null;
            for (int i = 0; i < depth; i++)
            {
                state = context.BlockStateStack.MaybePop();
            }
            if (state == null) return null;
            ScriptCommand cmd = state.BlockStart;
            BlockStatement stmt = (BlockStatement)cmd.Statement;
            while (stmt.StartsBlock)
            {
                cmd = stmt.GetMatchingEndCommand(context, script, cmd);
                stmt = (BlockStatement)cmd.Statement;
            }
            // when exiting a sub, we may not skip over the end command
            // which will transfer the control back to the caller
            if (state is CallSubState)
            {
                context.BlockStateStack.Push(state);
                return cmd;
            }
            return script.GetCommandAfter(cmd);
        }

        internal override string ToBeautifulString()
        {
            if (depth == 1) return "Exit";
            return "Exit " + depth;
        }
    }

    class EndStatement : BlockStatement
    {
        internal override bool StartsBlock { get { return false; } }
        internal override bool EndsBlock { get { return true; } }

        internal override void SkipCommand() { }

        internal override void Execute(ScriptContext context) { }

        internal override string GetCSharpExecute()
        {
            return null;
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            BlockState state = context.BlockStateStack.Pop();
            return state.GetCommandAfterEndStatement(context, script.GetCommandAfter(scriptCommand));
        }

        internal override string ToBeautifulString()
        {
            return "End";
        }
    }
    class RepeatStatement : ControlFlowStatement
    {
        readonly int count;

        internal RepeatStatement(int count) { this.count = count; }

        internal override void SkipCommand() { }
        internal override void Execute(ScriptContext context) { }

        internal override string GetCSharpExecute()
        {
            return "if (new ManagedWinapi.KeyboardKey(System.Windows.Forms.Keys.Pause).AsyncState == 0) {" +
                "repeatCount++;" +
                "if(repeatCount != " + count + ") goto repeatLabel;" +
                "}";
        }

        internal override ScriptCommand GetNextCommand(ScriptContext context, Script script, ScriptCommand scriptCommand)
        {
            if (RepeatEngine.ShouldRepeat(context, count))
            {
                return script.Commands[0];
            }
            else
            {
                return script.GetCommandAfter(scriptCommand);
            }
        }

        internal override string ToBeautifulString()
        {
            if (count == 0) return "Repeat";
            return "Repeat " + count;
        }
    }

    public static class RepeatEngine
    {
        public static bool ShouldRepeat(ScriptContext context, int count)
        {
            if (new KeyboardKey(Keys.Pause).AsyncState != 0) return false;
            context.RepeatCount++;
            if (count != 0 && context.RepeatCount == count) return false;
            return true;
        }
    }
}
