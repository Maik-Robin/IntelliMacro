using System;
using System.Collections.Generic;
using System.IO;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    class Parser
    {
        Lexer lexer;
        CommandRegistry registry;

        internal Parser(TextReader text) : this(text, false) { }

        internal Parser(TextReader text, bool recordComments)
        {
            this.lexer = new Lexer(text, recordComments);
            this.registry = CommandRegistry.Instance;
        }

        internal string RecordedComments { get { return lexer.RecordedComments; } }

        internal Script ParseScript()
        {
            Script script = new Script();
            while (true)
            {
                ScriptCommand cmd = ParseCommand();
                if (cmd == null) break;
                if (cmd.Label != null && script.ContainsLabel(cmd.Label))
                    throw new ParserException(cmd.LineNumber, cmd.Label.Length, "Duplicate label");
                script.AddCommand(cmd);
            }
            script.TestBlockCommands();
            return script;
        }

        internal Expression ParseExpression()
        {
            return ParseExpression(false);
        }

        internal ScriptCommand ParseCommand()
        {
            string label = null;
            Expression assignable = null;
            while (lexer.CurrentToken == ";") lexer.ConsumeToken();
            if (lexer.CurrentToken == "EOF") return null;
            Statement stmt = ParseStatement();
            int lineno = lexer.LineNumber;
            if (lexer.CurrentToken == ":")
            {
                if (stmt is IdentifierExpression == false)
                    throw new ParserException(lexer.LineNumber, lexer.Position, "Labels must be identifiers");
                label = ((IdentifierExpression)stmt).Identifier;
                lexer.ConsumeToken();
                stmt = ParseStatement();
            }
            if (lexer.CurrentToken == "=")
            {
                lexer.ConsumeToken();
                if (stmt is Expression == false || !((Expression)stmt).IsAssignable)
                    throw new ParserException(lexer.LineNumber, lexer.Position, "Cannot assign to this expression");
                assignable = (Expression)stmt;
                stmt = ParseExpression(false);
            }
            if (stmt is IdentifierExpression && assignable == null)
            {
                // is it a command?
                ICommand cmd = registry.GetCommand(((IdentifierExpression)stmt).Identifier);
                if (cmd != null && !cmd.ReturnsValue)
                {
                    stmt = new CommandStatement(cmd, ParseArguments(cmd.ParameterDescriptions));
                }
                else
                {
                    throw new ParserException(lexer.LineNumber, lexer.Position, "Variable value not used");
                }
            }
            if (lexer.CurrentToken != ";")
                throw new ParserException(lexer.LineNumber, lexer.Position, "End of line expected, found " + lexer.CurrentToken);
            return new ScriptCommand(lineno, label, assignable, stmt);
        }

        private Statement ParseStatement()
        {
            if (lexer.CurrentToken.StartsWith("I"))
            {
                switch (lexer.CurrentToken.Substring(1).ToLowerInvariant())
                {
                    case "goto":
                        lexer.ConsumeToken();
                        string label = lexer.ConsumeToken();
                        Expression condition = null, dynamicLabel = null;
                        if (label == "(")
                        {
                            dynamicLabel = ParseExpression(false);
                            lexer.ExpectToken(")");
                        }
                        else if (!label.StartsWith("I"))
                            throw new ParserException(lexer.LineNumber, lexer.Position, "Labels must be identifiers");
                        if (lexer.CurrentToken.ToLowerInvariant() == "iif")
                        {
                            lexer.ConsumeToken();
                            condition = ParseExpression(false);
                        }
                        return new GotoStatement(label.Substring(1), condition, dynamicLabel);
                    case "for":
                        lexer.ConsumeToken();
                        Expression assignable = ParseExpression(true);
                        if (!assignable.IsAssignable)
                            throw new ParserException(lexer.LineNumber, lexer.Position, "Cannot assign to this expression");
                        if (lexer.CurrentToken == ":")
                        {
                            lexer.ConsumeToken();
                            Expression iterable = ParseExpression(false);
                            return new ForStatement(assignable, iterable);
                        }
                        else if (lexer.CurrentToken == "=")
                        {
                            lexer.ConsumeToken();
                            Expression from = ParseExpression(false);
                            lexer.ExpectToken("..");
                            Expression to = ParseExpression(false);
                            return new ForStatement(assignable, from, to);
                        }
                        else
                        {
                            throw new ParserException(lexer.LineNumber, lexer.Position, "':' or '=' expected");
                        }
                    case "while":
                        lexer.ConsumeToken();
                        return new WhileStatement(ParseExpression(false));
                    case "if":
                        lexer.ConsumeToken();
                        return new IfStatement(ParseExpression(false));
                    case "elseif":
                        lexer.ConsumeToken();
                        return new ElseIfStatement(ParseExpression(false));
                    case "else":
                        lexer.ConsumeToken();
                        return new ElseIfStatement(new ConstantExpression(MacroObject.ONE));
                    case "end":
                        lexer.ConsumeToken();
                        return new EndStatement();
                    case "exit":
                        lexer.ConsumeToken();
                        if (lexer.CurrentToken.StartsWith("N"))
                        {
                            return new ExitStatement(int.Parse(lexer.ConsumeToken().Substring(1)));
                        }
                        return new ExitStatement(1);
                    case "repeat":
                        lexer.ConsumeToken();
                        if (lexer.CurrentToken.StartsWith("N"))
                        {
                            return new RepeatStatement(int.Parse(lexer.ConsumeToken().Substring(1)));
                        }
                        return new RepeatStatement(0);
                    case "sub":
                        lexer.ConsumeToken();
                        string subname = lexer.ConsumeToken();
                        if (!subname.StartsWith("I"))
                            throw new ParserException(lexer.LineNumber, lexer.Position, "Procedure names must be identifiers");
                        lexer.ExpectToken("(");
                        List<string> variables = new List<string>();
                        while (lexer.CurrentToken != ")")
                        {
                            if (variables.Count != 0)
                                lexer.ExpectToken(",");
                            string vartype = "";
                            if (lexer.CurrentToken == "&")
                            {
                                vartype = "&";
                                lexer.ConsumeToken();
                            }
                            string var = lexer.ConsumeToken();
                            if (!var.StartsWith("I"))
                                throw new ParserException(lexer.LineNumber, lexer.Position, "Procedure parameters must be identifiers");
                            var = var.Substring(1).ToLowerInvariant();
                            if (variables.Contains(var) || variables.Contains("&" + var))
                            {
                                throw new ParserException(lexer.LineNumber, lexer.Position, "Duplicate parameter name '" + var + "'");
                            }
                            variables.Add(vartype + var);
                        }
                        lexer.ExpectToken(")");
                        return new SubStatement(subname.Substring(1), variables);
                    case "call":
                        lexer.ConsumeToken();
                        string callsubname = lexer.ConsumeToken();
                        if (!callsubname.StartsWith("I"))
                            throw new ParserException(lexer.LineNumber, lexer.Position, "Procedure names must be identifiers");
                        lexer.ExpectToken("(");
                        List<Expression> parameters = new List<Expression>();
                        while (lexer.CurrentToken != ")")
                        {
                            if (parameters.Count != 0)
                                lexer.ExpectToken(",");
                            parameters.Add(ParseExpression(false));
                        }
                        lexer.ExpectToken(")");
                        return new CallStatement(callsubname.Substring(1), parameters);
                    default:
                        ICommand cmd = registry.GetCommand(lexer.CurrentToken.Substring(1));
                        if (cmd != null && !cmd.ReturnsValue)
                        {
                            // so that stuff like WHEEL -120 works as expected
                            return ParseAtomExpression();
                        }
                        break;
                }
            }
            else if (lexer.CurrentToken == "#")
            {
                // treat this line as a comment
                lexer.SkipRemainingLine();
                return new VoidStatement();
            }
            else if (lexer.CurrentToken == ";" || lexer.CurrentToken == "EOF")
            {
                return new VoidStatement();
            }
            return ParseExpression(true);
        }

        private Expression ParseExpression(bool toplevel)
        {
            IList<string> compareTokens = new String[] {
                 "<", ">","=", "<=", ">=", "!=", "<>"
            };
            CompareFlags[] compareFlags = {
                CompareFlags.Less,
                CompareFlags.Greater,
                CompareFlags.StringCompare | CompareFlags.Equals,
                CompareFlags.Less | CompareFlags.Equals,
                CompareFlags.Greater | CompareFlags.Equals,
                CompareFlags.StringCompare | CompareFlags.Greater | CompareFlags.Less,
                CompareFlags.Greater | CompareFlags.Less
            };
            Expression expr = ParseBinaryExpression(0);
            while (true)
            {
                if (lexer.CurrentToken == "=" && toplevel)
                {
                    return expr;
                }
                else if (compareTokens.Contains(lexer.CurrentToken))
                {
                    string token = lexer.ConsumeToken();
                    Expression expr2 = ParseBinaryExpression(0);
                    expr = new CompareExpression(expr, token, compareFlags[compareTokens.IndexOf(token)], expr2);
                }
                else
                {
                    return expr;
                }
            }
        }

        private Expression ParseBinaryExpression(int depth)
        {
            string tokens;
            switch (depth)
            {
                case 0: tokens = "&X"; break;
                case 1: tokens = "+-"; break;
                case 2: tokens = "*/%"; break;
                case 3: tokens = "^"; break;
                case 4: return ParsePrefixExpression();
                default: throw new ArgumentException();
            }
            Expression expr = ParseBinaryExpression(depth + 1);
            while (true)
            {
                String token = lexer.CurrentToken;
                if (token == "&&") token = "X";
                if (token.Length == 1 && tokens.Contains(token))
                {
                    lexer.ConsumeToken();
                    Expression expr2 = ParseBinaryExpression(depth + 1);
                    expr = new BinaryExpression(expr, token[0], expr2);
                }
                else
                {
                    return expr;
                }
            }
        }

        private Expression ParsePrefixExpression()
        {
            switch (lexer.CurrentToken)
            {
                case "#":
                case "~":
                case "$":
                case "!":
                case "-":
                case "%":
                    char token = lexer.CurrentToken[0];
                    lexer.ConsumeToken();
                    return new PrefixExpression(token, ParsePrefixExpression());
                default:
                    return ParsePostfixExpression();
            }
        }

        private Expression ParsePostfixExpression()
        {
            Expression expr = ParseAtomExpression();
            while (true)
            {
                if (lexer.CurrentToken == "[")
                {
                    lexer.ConsumeToken();
                    Expression expr2, expr3 = null;
                    if (lexer.CurrentToken == "<")
                    {
                        lexer.ConsumeToken();
                        expr2 = expr3 = new ConstantExpression(int.MinValue);
                    }
                    else if (lexer.CurrentToken == ">")
                    {
                        lexer.ConsumeToken();
                        expr2 = expr3 = new ConstantExpression(int.MaxValue);
                    }
                    else
                    {
                        expr2 = lexer.CurrentToken == ".." ? new ImplicitBoundsExpression(1) : ParseExpression(false);
                        if (lexer.CurrentToken == "..")
                        {
                            lexer.ConsumeToken();
                            expr3 = lexer.CurrentToken == "]" ? new ImplicitBoundsExpression(-1) : ParseExpression(false);
                        }
                    }
                    lexer.ExpectToken("]");
                    if (expr3 == null)
                        expr = new SubscriptExpression(expr, expr2);
                    else
                        expr = new SliceExpression(expr, expr2, expr3);
                }
                else if (lexer.CurrentToken == "(")
                {
                    if (expr is IdentifierExpression == false)
                        return expr;
                    string identifier = ((IdentifierExpression)expr).Identifier;
                    ICommand cmd = registry.GetCommand(identifier);
                    if (cmd == null || !cmd.ReturnsValue) return expr;
                    lexer.ConsumeToken();
                    expr = new CommandExpression(cmd, ParseArguments(cmd.ParameterDescriptions));
                    lexer.ExpectToken(")");
                }
                else
                {
                    return expr;
                }
            }
        }

        private Expression ParseAtomExpression()
        {
            if (lexer.CurrentToken == "(")
            {
                lexer.ConsumeToken();
                // wrap the expression here so that parentheses will be
                // reproduced when prettyprinting
                Expression result = new ParenExpression(ParseExpression(false));
                lexer.ExpectToken(")");
                return result;
            }
            else if (lexer.CurrentToken == "[")
            {
                lexer.ConsumeToken();
                List<Expression[]> ranges = new List<Expression[]>();
                while (lexer.CurrentToken != "]")
                {
                    if (ranges.Count > 0)
                        lexer.ExpectToken(",");
                    Expression expr = ParseExpression(false);
                    if (lexer.CurrentToken == "..")
                    {
                        lexer.ConsumeToken();
                        ranges.Add(new Expression[] { expr, ParseExpression(false) });
                    }
                    else
                    {
                        ranges.Add(new Expression[] { expr });
                    }
                }
                lexer.ExpectToken("]");
                return new ListExpression(ranges);
            }
            else if (lexer.CurrentToken.StartsWith("N"))
            {
                string number = lexer.ConsumeToken().Substring(1);
                long result;
                if (long.TryParse(number, out result))
                    return new ConstantExpression(result);
                throw new ParserException(lexer.LineNumber, lexer.Position, "Overflow: " + number);
            }
            else if (lexer.CurrentToken.StartsWith("S"))
            {
                return new ConstantExpression(lexer.ConsumeToken().Substring(1));
            }
            else if (lexer.CurrentToken.StartsWith("K"))
            {
                try
                {
                    return new KeyNameExpression(lexer.ConsumeToken().Substring(1));
                }
                catch (MacroErrorException ex)
                {
                    throw new ParserException(lexer.LineNumber, lexer.Position, ex.Message);
                }
            }
            else if (lexer.CurrentToken.StartsWith("I"))
            {
                return new IdentifierExpression(lexer.ConsumeToken().Substring(1));
            }
            else
            {
                throw new ParserException(lexer.LineNumber, lexer.Position,
                    lexer.CurrentToken == ";" ? "Unexpected end of line" :
                    "Unexpected token: " + lexer.CurrentToken);
            }
        }

        private Expression[] ParseArguments(ParameterDescription[] parameterDescription)
        {
            Expression[] result = new Expression[parameterDescription.Length];
            for (int i = 0; i < result.Length; i++)
            {
                if (lexer.CurrentToken == ")" || lexer.CurrentToken == ";")
                {
                    bool allOptional = true;
                    for (int j = i; j < result.Length; j++)
                    {
                        if (!parameterDescription[j].Optional) allOptional = false;
                    }
                    if (allOptional) break;
                }
                if (i != 0)
                    lexer.ExpectToken(",");
                if (lexer.CurrentToken == "," || lexer.CurrentToken == ")" || lexer.CurrentToken == ";")
                {
                    if (parameterDescription[i].Optional) continue;
                }
                result[i] = ParseExpression(false);
            }
            return result;
        }
    }
}
