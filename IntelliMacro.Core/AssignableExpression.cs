using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using IntelliMacro.Runtime;
using System.Globalization;

namespace IntelliMacro.Core
{
    class IdentifierExpression : Expression
    {
        private readonly string identifier;
        private string cSharpIdentifier = null;

        internal string Identifier { get { return identifier; } }

        internal IdentifierExpression(string identifier)
        {
            if (!Regex.IsMatch(identifier, "^[A-Za-z0-9_\u0081-\uFFFF]+$")) throw new ArgumentException();
            this.identifier = identifier;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            return context.GetVariable(identifier);
        }

        internal override string GetCSharpEvaluate()
        {
            if (cSharpIdentifier == null)
            {
                if (identifier.ToLowerInvariant().StartsWith("g_"))
                    cSharpIdentifier = "gv" + BuildCSharpIdentifier(identifier.Substring(2));
                else
                    cSharpIdentifier = "v" + BuildCSharpIdentifier(identifier);
            }
            return cSharpIdentifier;
        }

        internal override bool IsAssignable
        {
            get { return true; }
        }

        internal override void Assign(ScriptContext context, MacroObject value)
        {
            context.SetVariable(identifier, value);
        }

        internal override string GetCSharpAssign(string value)
        {
            return GetCSharpEvaluate() + " = " + value;
        }

        internal override string ToBeautifulString()
        {
            return identifier;
        }

        internal override void AddVariables(IList<string> variables)
        {
            variables.Add(GetCSharpEvaluate());
        }

        internal static string BuildCSharpIdentifier(string identifier)
        {
            identifier = identifier.ToLowerInvariant();
            StringBuilder allowedChars = new StringBuilder();
            StringBuilder disallowedChars = new StringBuilder();
            foreach (char c in identifier)
            {
                UnicodeCategory cat = char.GetUnicodeCategory(c);
                if (c == '_' || cat == UnicodeCategory.UppercaseLetter
                    || cat == UnicodeCategory.LowercaseLetter
                    || cat == UnicodeCategory.TitlecaseLetter
                    || cat == UnicodeCategory.ModifierLetter
                    || cat == UnicodeCategory.OtherLetter
                    || cat == UnicodeCategory.LetterNumber
                    || cat == UnicodeCategory.DecimalDigitNumber)
                {
                    allowedChars.Append(c);
                }
                else
                {
                    disallowedChars.Append(disallowedChars.Length).Append('x').Append((c & 0xFFFF).ToString("X")).Append('X');
                }
            }
            if (disallowedChars.Length == 0)
            {
                return "_" + identifier;
            }
            else
            {
                return "x_" + disallowedChars + "_" + allowedChars.ToString();
            }
        }
    }

    class SubscriptExpression : Expression
    {
        Expression expr, subscript;
        public SubscriptExpression(Expression expr, Expression subscript)
        {
            this.expr = expr;
            this.subscript = subscript;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            return expr.Evaluate(context)[subscript.Evaluate(context)];
        }

        internal override string GetCSharpEvaluate()
        {
            return expr.GetCSharpEvaluate() + "[" + subscript.GetCSharpEvaluate() + "]";
        }

        internal override bool IsAssignable
        {
            get
            {
                return expr.IsAssignable;
            }
        }

        internal override void Assign(ScriptContext context, MacroObject value)
        {
            if (!IsAssignable)
                base.Assign(context, value);
            expr.Assign(context, expr.Evaluate(context).SetItem(subscript.Evaluate(context), value));
        }

        internal override string GetCSharpAssign(string value)
        {
            if (!IsAssignable) return base.GetCSharpAssign(value);

            return expr.GetCSharpAssign("(" + expr.GetCSharpEvaluate() + ").SetItem(" + subscript.GetCSharpEvaluate() + ", " + value + ")");
        }

        internal override string ToBeautifulString()
        {
            return expr.ToBeautifulString() + "[" + subscript.ToBeautifulString() + "]";
        }

        internal override void AddVariables(IList<string> variables)
        {
            expr.AddVariables(variables);
            subscript.AddVariables(variables);
        }

    }

    class SliceExpression : Expression
    {
        Expression expr, sliceStart, sliceEnd;

        public SliceExpression(Expression expr, Expression sliceStart, Expression sliceEnd)
        {
            this.expr = expr;
            this.sliceStart = sliceStart;
            this.sliceEnd = sliceEnd;
        }
        internal override MacroObject Evaluate(ScriptContext context)
        {
            return expr.Evaluate(context).GetSlice(sliceStart.Evaluate(context), sliceEnd.Evaluate(context));
        }

        internal override string GetCSharpEvaluate()
        {
            return expr.GetCSharpEvaluate() + ".GetSlice(" + sliceStart.GetCSharpEvaluate() + ", " + sliceEnd.GetCSharpEvaluate() + ")";
        }

        internal override bool IsAssignable
        {
            get
            {
                return expr.IsAssignable;
            }
        }

        internal override void Assign(ScriptContext context, MacroObject value)
        {
            if (!IsAssignable)
                base.Assign(context, value);
            expr.Assign(context, expr.Evaluate(context).SetSlice(sliceStart.Evaluate(context), sliceEnd.Evaluate(context), value));
        }

        internal override string GetCSharpAssign(string value)
        {
            if (!IsAssignable) return base.GetCSharpAssign(value);

            return expr.GetCSharpAssign("(" + expr.GetCSharpEvaluate() + ").SetSlice(" + sliceStart.GetCSharpEvaluate() + ", " + sliceEnd.GetCSharpEvaluate() + ", " + value + ")");
        }

        internal override string ToBeautifulString()
        {
            if (isConstant(sliceStart, int.MaxValue) && isConstant(sliceEnd, int.MaxValue))

                return expr.ToBeautifulString() + "[>]";
            if (isConstant(sliceStart, int.MinValue) && isConstant(sliceEnd, int.MinValue))

                return expr.ToBeautifulString() + "[<]";

            return expr.ToBeautifulString() + "[" + sliceStart.ToBeautifulString() +
                (sliceStart is ImplicitBoundsExpression ? "" : " ") + ".." +
                (sliceEnd is ImplicitBoundsExpression ? "" : " ") +
                sliceEnd.ToBeautifulString() + "]";
        }

        private bool isConstant(Expression expression, long value)
        {
            if (expression is ConstantExpression)
            {
                MacroObject obj = ((ConstantExpression)expression).Evaluate(null);
                return obj.IsNumber && obj.Number == value;
            }
            else
            {
                return false;
            }
        }

        internal override void AddVariables(IList<string> variables)
        {
            expr.AddVariables(variables);
            sliceStart.AddVariables(variables);
            sliceEnd.AddVariables(variables);
        }
    }

    class ListExpression : Expression
    {
        IList<Expression[]> ranges;
        public ListExpression(IList<Expression[]> ranges)
        {
            this.ranges = ranges;
        }

        internal override MacroObject Evaluate(ScriptContext context)
        {
            List<MacroObject> elems = new List<MacroObject>();
            foreach (Expression[] range in ranges)
            {
                if (range.Length == 1)
                {
                    elems.Add(range[0].Evaluate(context));
                }
                else
                {
                    new MacroObjectRange(range[0].Evaluate(context), range[1].Evaluate(context)).AppendTo(elems);
                }
            }
            return new MacroList(elems);
        }

        internal override string GetCSharpEvaluate()
        {
            StringBuilder sb = new StringBuilder("r.BuildList(");
            bool first = true;
            foreach (Expression[] range in ranges)
            {
                if (!first) sb.Append(", ");
                first = false;
                if (range.Length == 1)
                {
                    sb.Append(range[0].GetCSharpEvaluate());
                }
                else
                {
                    sb.Append("new MacroObjectRange(" + range[0].GetCSharpEvaluate() + ", " + range[1].GetCSharpEvaluate() + ")");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        internal override bool IsAssignable
        {
            get
            {
                for (int i = 0; i < ranges.Count; i++)
                {
                    Expression[] range = ranges[i];
                    bool last = (i == ranges.Count - 1);
                    if (!range[0].IsAssignable) return false;
                    if (range.Length > 1)
                    {
                        if (!last || !range[1].IsAssignable) return false;
                    }
                }
                return true;
            }
        }

        internal override void Assign(ScriptContext context, MacroObject value)
        {
            if (!IsAssignable)
                base.Assign(context, value);
            int cnt = ranges.Count;
            for (int i = 0; i < cnt; i++)
            {
                ranges[i][0].Assign(context, value[i + 1]);
            }

            if (ranges[cnt - 1].Length > 1)
            {
                ranges[cnt - 1][1].Assign(context, value.GetSlice(cnt + 1, -1));
            }
        }

        static int listCounter = 0;

        internal override string GetCSharpAssign(string value)
        {
            if (!IsAssignable) return base.GetCSharpAssign(value);
            listCounter++;
            string result = "MacroObject list_" + listCounter + " = " + value;
            int cnt = ranges.Count;
            for (int i = 0; i < cnt; i++)
            {
                result = result + ";" + ranges[i][0].GetCSharpAssign("list_" + listCounter + "[(MacroObject)" + (i + 1) + "]");
            }
            if (ranges[cnt - 1].Length > 1)
            {
                result = result + ";" + ranges[cnt - 1][1].GetCSharpAssign("list_" + listCounter + ".GetSlice((MacroObject)" + (cnt + 1) + ", (MacroObject)(-1))");
            }
            return result;
        }

        internal override string ToBeautifulString()
        {
            StringBuilder sb = new StringBuilder("[");
            bool first = true;
            foreach (Expression[] range in ranges)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");
                sb.Append(range[0].ToBeautifulString());
                if (range.Length == 2)
                    sb.Append(" .. ").Append(range[1].ToBeautifulString());
            }
            return sb.Append("]").ToString();
        }

        internal override void AddVariables(IList<string> variables)
        {
            foreach (Expression[] range in ranges)
            {
                foreach (Expression expr in range)
                {
                    expr.AddVariables(variables);
                }
            }
        }
    }
}