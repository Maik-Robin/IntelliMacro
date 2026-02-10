using System;
using System.Collections.Generic;
using System.Text;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    abstract class Expression : Statement
    {
        internal virtual bool IsAssignable { get { return false; } }
        internal virtual void Assign(ScriptContext context, MacroObject value)
        {
            throw new Exception();
        }
        internal virtual string GetCSharpAssign(string value)
        {
            throw new Exception();
        }

        internal override void Execute(ScriptContext context, out int delay, out IMacroEvent waitEvent)
        {
            Evaluate(context);
            delay = 0;
            waitEvent = null;
        }

        internal override string GetCSharpExecute()
        {
            return GetCSharpEvaluate() + ";";
        }

        internal abstract MacroObject Evaluate(ScriptContext context);
        internal abstract string GetCSharpEvaluate();
    }

    [Flags]
    enum CompareFlags
    {
        StringCompare = 1,
        Less = 2,
        Equals = 4,
        Greater = 8
    }

    class CompareExpression : Expression
    {
        Expression e1, e2;
        string token;
        CompareFlags flags;

        public CompareExpression(Expression e1, string token, CompareFlags flags, Expression e2)
        {
            this.e1 = e1;
            this.token = token;
            this.flags = flags;
            this.e2 = e2;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            MacroObject o1 = e1.Evaluate(context);
            MacroObject o2 = e2.Evaluate(context);
            long result;
            if ((flags & CompareFlags.StringCompare) != 0)
            {
                result = o1.String.CompareTo(o2.String);
            }
            else
            {
                result = o1.Number - o2.Number;
            }
            CompareFlags flag;
            if (result < 0) flag = CompareFlags.Less;
            else if (result > 0) flag = CompareFlags.Greater;
            else flag = CompareFlags.Equals;
            if ((flags & flag) != 0)
            {
                return MacroObject.ONE;
            }
            else
            {
                return MacroObject.ZERO;
            }
        }

        internal override string GetCSharpEvaluate()
        {
            string o1 = e1.GetCSharpEvaluate();
            string o2 = e2.GetCSharpEvaluate();
            switch (flags)
            {
                case CompareFlags.Less:
                    return "(" + o1 + ".Number < " + o2 + ".Number ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.Greater:
                    return "(" + o1 + ".Number > " + o2 + ".Number ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.StringCompare | CompareFlags.Equals:
                    return "(" + o1 + ".String == " + o2 + ".String ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.Less | CompareFlags.Equals:
                    return "(" + o1 + ".Number <= " + o2 + ".Number ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.Greater | CompareFlags.Equals:
                    return "(" + o1 + ".Number >= " + o2 + ".Number ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.StringCompare | CompareFlags.Greater | CompareFlags.Less:
                    return "(" + o1 + ".String != " + o2 + ".String ? (MacroObject)1 : (MacroObject)0)";
                case CompareFlags.Greater | CompareFlags.Less:
                    return "(" + o1 + ".Number != " + o2 + ".Number ? (MacroObject)1 : (MacroObject)0)";
                default: throw new Exception();
            }
        }

        internal override string ToBeautifulString()
        {
            return e1.ToBeautifulString() + " " + token + " " + e2.ToBeautifulString();
        }

        internal override void AddVariables(IList<string> variables)
        {
            e1.AddVariables(variables);
            e2.AddVariables(variables);
        }
    }

    class BinaryExpression : Expression
    {
        Expression e1, e2;
        char op;

        public BinaryExpression(Expression e1, char op, Expression e2)
        {
            this.e1 = e1;
            this.e2 = e2;
            this.op = op;
        }
        internal override MacroObject Evaluate(ScriptContext context)
        {
            MacroObject o1 = e1.Evaluate(context);
            MacroObject o2 = e2.Evaluate(context);
            switch (op)
            {
                case '+': return o1 + o2;
                case '-': return o1 - o2;
                case '*': return o1 * o2;
                case '/': return o1 / o2;
                case '%': return o1 % o2;
                case '^': return o1.Pow(o2);
                case '&': return o1.Concat(o2);
                case 'X': return o1.ConcatNL(o2);
                default: throw new Exception();
            }
        }

        internal override string GetCSharpEvaluate()
        {
            string o1 = e1.GetCSharpEvaluate();
            string o2 = e2.GetCSharpEvaluate();
            switch (op)
            {
                case '+': return "(" + o1 + " + " + o2 + ")";
                case '-': return "(" + o1 + " - " + o2 + ")";
                case '*': return "(" + o1 + " * " + o2 + ")";
                case '/': return "(" + o1 + " / " + o2 + ")";
                case '%': return "(" + o1 + " % " + o2 + ")";
                case '^': return o1 + ".Pow(" + o2 + ")";
                case '&': return o1 + ".Concat(" + o2 + ")";
                case 'X': return o1 + ".ConcatNL(" + o2 + ")";
                default: throw new Exception();
            }
        }

        internal override string ToBeautifulString()
        {
            return e1.ToBeautifulString() + " " +
                (op == 'X' ? "&&" : "" + op) + " " + e2.ToBeautifulString();
        }

        internal override void AddVariables(IList<string> variables)
        {
            e1.AddVariables(variables);
            e2.AddVariables(variables);
        }
    }

    class PrefixExpression : Expression
    {
        char token;
        Expression expr;
        public PrefixExpression(char token, Expression expr)
        {
            this.token = token;
            this.expr = expr;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            MacroObject o = expr.Evaluate(context);
            switch (token)
            {
                case '#': return o.MakeCharacter();
                case '~': return context.MacroContext.Random.Next((int)o.Number);
                case '$': return o.Length;
                case '!': return o.Negate();
                case '-': return -o;
                case '%': return o.Type;
                default: throw new Exception();
            }
        }

        internal override string GetCSharpEvaluate()
        {
            string o = expr.GetCSharpEvaluate();
            switch (token)
            {
                case '#': return o + ".MakeCharacter()";
                case '~': return "((MacroObject)r.NextRandom(" + o + ".Number))";
                case '$': return "((MacroObject)" + o + ".Length)";
                case '!': return o + ".Negate()";
                case '-': return "(-" + o + ")";
                case '%': return "((MacroObject)" + o + ".Type)";
                default: throw new Exception();
            }
        }

        internal override string ToBeautifulString()
        {
            return token + expr.ToBeautifulString();
        }

        internal override void AddVariables(IList<string> variables)
        {
            expr.AddVariables(variables);
        }
    }

    class CommandExpression : Expression
    {
        protected ICommand command;
        protected Expression[] parameters;
        internal CommandExpression(ICommand command, Expression[] parameters) : this(command, parameters, true) { }

        protected CommandExpression(ICommand command, Expression[] parameters, bool useReturnValue)
        {
            ParameterDescription[] descs = command.ParameterDescriptions;
            if (command.ReturnsValue != useReturnValue) throw new ArgumentException();
            if (descs.Length != parameters.Length) throw new ArgumentException();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] == null && !descs[i].Optional)
                    throw new ArgumentException();
            }
            this.command = command;
            this.parameters = parameters;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            MacroObject[] p = new MacroObject[parameters.Length];
            for (int i = 0; i < p.Length; i++)
            {
                p[i] = parameters[i] == null ? null : parameters[i].Evaluate(context);
            }
            return command.Invoke(context.MacroContext, p);
        }

        internal override string GetCSharpEvaluate()
        {
            StringBuilder result = new StringBuilder();
            result.Append("r.Invoke(" + CommandRegistry.Instance.GetDeclaringType(command.Name).Name + "." + command.Name);
            foreach (Expression parameter in parameters)
            {
                result.Append(", ");
                if (parameter == null)
                    result.Append("(MacroObject)null");
                else
                    result.Append(parameter.GetCSharpEvaluate());
            }
            result.Append(")");
            return result.ToString();
        }

        internal override string ToBeautifulString()
        {
            StringBuilder sb = new StringBuilder(command.Name);
            sb.Append(command.ReturnsValue ? "(" : " ");
            int paramCount = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] != null) paramCount = i + 1;
            }
            for (int i = 0; i < paramCount; i++)
            {
                if (i != 0) sb.Append(", ");
                if (parameters[i] != null) sb.Append(parameters[i].ToBeautifulString());
            }
            sb.Append(command.ReturnsValue ? ")" : "");
            return sb.ToString();
        }

        internal override void AddDeclaringTypes(IList<Type> types)
        {
            types.Add(CommandRegistry.Instance.GetDeclaringType(command.Name));
        }

        internal override void AddVariables(IList<string> variables)
        {
            foreach (Expression parameter in parameters)
            {
                if (parameter != null)
                    parameter.AddVariables(variables);
            }
        }
    }

    class ConstantExpression : Expression
    {
        MacroObject constant;
        public ConstantExpression(MacroObject constant)
        {
            this.constant = constant;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            return constant;
        }

        internal override string GetCSharpEvaluate()
        {
            if (constant.IsNumber)
            {
                if (constant.Number < 0)
                    return "((MacroObject)(" + constant.Number.ToString() + "))";
                else
                    return "((MacroObject)" + constant.Number.ToString() + ")";
            }
            else
            {
                return "((MacroObject)@\"" + constant.String.Replace("\"", "\"\"") + "\")";
            }
        }

        internal override string ToBeautifulString()
        {
            if (constant.IsNumber)
            {
                return constant.Number.ToString();
            }
            else
            {
                return "\"" + constant.String.Replace("\"", "\"\"") + "\"";
            }
        }
    }

    class KeyNameExpression : ConstantExpression
    {
        string keyName;
        public KeyNameExpression(string keyName)
            : base(MacroObject.FromKey(keyName))
        {
            this.keyName = keyName;
        }

        internal override string ToBeautifulString()
        {
            return "<" + keyName.ToUpperInvariant() + ">";
        }

        internal override string GetCSharpEvaluate()
        {
            return "MacroObject.FromKey(\"" + keyName + "\")";
        }
    }

    class ImplicitBoundsExpression : ConstantExpression
    {
        public ImplicitBoundsExpression(int value) : base(value) { }

        internal override string ToBeautifulString() { return ""; }
    }

    class ParenExpression : Expression
    {
        Expression inside;
        public ParenExpression(Expression inside) { this.inside = inside; }
        internal override MacroObject Evaluate(ScriptContext context) { return inside.Evaluate(context); }

        internal override string GetCSharpEvaluate()
        {
            return "(" + inside.GetCSharpEvaluate() + ")";
        }
        internal override string ToBeautifulString()
        {
            return "(" + inside.ToBeautifulString() + ")";
        }

        internal override void AddVariables(IList<string> variables)
        {
            inside.AddVariables(variables);
        }
    }
}
