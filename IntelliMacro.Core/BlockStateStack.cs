using System.Collections.Generic;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    class BlockStateStack
    {

        List<BlockState> stack = new List<BlockState>();
        internal BlockStateStack() { }

        internal BlockState Top { get { return stack[stack.Count - 1]; } }

        internal BlockState MaybePop()
        {
            if (stack.Count == 0) return null;
            return Pop();
        }

        internal BlockState Pop()
        {
            BlockState result = stack[stack.Count - 1];
            stack.RemoveAt(stack.Count - 1);
            return result;
        }

        internal void Push(BlockState state)
        {
            stack.Add(state);
        }
    }

    abstract class BlockState
    {
        ScriptCommand blockStart;

        protected BlockState(ScriptCommand blockStart)
        {
            this.blockStart = blockStart;
        }

        public ScriptCommand BlockStart { get { return blockStart; } }

        internal abstract ScriptCommand GetCommandAfterEndStatement(ScriptContext context, ScriptCommand nextCommand);
    }

    class IfState : BlockState
    {
        public bool wasEntered;
        internal IfState(ScriptCommand blockStart, bool wasEntered)
            : base(blockStart)
        {
            this.wasEntered = wasEntered;
        }

        internal override ScriptCommand GetCommandAfterEndStatement(ScriptContext context, ScriptCommand nextCommand)
        {
            return nextCommand;
        }
    }

    class WhileState : BlockState
    {
        protected readonly bool wasExecuted;

        internal WhileState(ScriptCommand blockStart, bool wasExecuted)
            : base(blockStart)
        {
            this.wasExecuted = wasExecuted;
        }

        internal override ScriptCommand GetCommandAfterEndStatement(ScriptContext context, ScriptCommand nextCommand)
        {
            if (wasExecuted)
                return BlockStart;
            else
                return nextCommand;
        }
    }

    class ForState : WhileState
    {
        MacroObject remaining = null;

        internal ForState(ScriptCommand blockStart, bool wasExecuted, MacroObject remaining)
            : base(blockStart, wasExecuted)
        {
            this.remaining = remaining;
        }

        internal override ScriptCommand GetCommandAfterEndStatement(ScriptContext context, ScriptCommand nextCommand)
        {
            if (wasExecuted)
            {
                ((ForStatement)BlockStart.Statement).remaining = remaining;
            }
            return base.GetCommandAfterEndStatement(context, nextCommand);
        }
    }

    class CallSubState : BlockState
    {
        private readonly CallSubContext context;
        private readonly ScriptCommand returnCommand;
        internal CallSubState(ScriptCommand blockStart, CallSubContext context, ScriptCommand returnCommand)
            : base(blockStart)
        {
            this.context = context;
            this.returnCommand = returnCommand;
        }

        internal override ScriptCommand GetCommandAfterEndStatement(ScriptContext scriptContext, ScriptCommand nextCommand)
        {
            Dictionary<string, MacroObject> refValues = new Dictionary<string, MacroObject>();
            foreach (string refVar in context.RefParameters.Keys)
            {
                refValues.Add(refVar, scriptContext.GetVariable(refVar));
            }
            scriptContext.ExtractLocalVariables();
            foreach (KeyValuePair<string, MacroObject> local in context.LocalVariables)
            {
                scriptContext.SetVariable(local.Key, local.Value);
            }
            foreach (KeyValuePair<string, Expression> refExpr in context.RefParameters)
            {
                refExpr.Value.Assign(scriptContext, refValues[refExpr.Key]);
            }
            return returnCommand;
        }
    }

    class CallSubContext
    {
        private readonly SubStatement statement;
        private readonly IDictionary<string, MacroObject> localVariables;
        private readonly IDictionary<string, Expression> refParameters = new Dictionary<string, Expression>();

        internal CallSubContext(SubStatement statement, IDictionary<string, MacroObject> localVariables)
        {
            this.statement = statement;
            this.localVariables = localVariables;
        }

        internal SubStatement SubStatement { get { return statement; } }
        internal IDictionary<string, MacroObject> LocalVariables { get { return localVariables; } }
        internal IDictionary<string, Expression> RefParameters { get { return refParameters; } }
    }
}
