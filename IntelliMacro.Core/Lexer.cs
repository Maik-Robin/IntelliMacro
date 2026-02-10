using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace IntelliMacro.Core
{
    class Lexer
    {
        TextReader input;
        string currentLine = null;
        int lineNumber = 0, baseLineNumber = 0, pos = 0, startPos;
        string currentToken = null;
        StringBuilder comments = null;
        internal Lexer(TextReader input, bool recordComments)
        {
            this.input = input;
            if (recordComments) comments = new StringBuilder();
        }

        public int LineNumber { get { return lineNumber; } }
        public int Position { get { return startPos; } }

        internal string RecordedComments { get { return comments.ToString(); } }

        static readonly String[] symbolTokens = { 
            "&&", ">=", "!=", "<=", "<>", "..",
            "=", ">", "(", ")", "[", "]", "+", "-", "*", "/", 
            "%", "^", "~", "$", "&", "<", ":", "!", ",", ";"
        };

        public string CurrentToken
        {
            get
            {
                if (currentToken == null)
                {
                    startPos = pos;
                    currentToken = GetNextToken();
                }
                return currentToken;
            }
        }

        public string ConsumeToken()
        {
            string result = CurrentToken;
            currentToken = null;
            return result;
        }

        public void ExpectToken(string token)
        {
            if (ConsumeToken() != token)
                throw new ParserException(LineNumber, Position, "'" + token + "' expected");
        }

        internal void SkipRemainingLine()
        {
            if (comments != null)
                comments.Append(currentLine.Substring(pos));
            pos = currentLine.Length;
            currentToken = null;
        }

        private string GetNextToken()
        {
            if (currentLine == null)
            {
                baseLineNumber++;
                lineNumber = baseLineNumber;
                pos = 0;
                currentLine = input.ReadLine();
                if (currentLine == null) return "EOF";
            }
            while (true)
            {
                if (pos == currentLine.Length)
                {
                    currentLine = null;
                    return ";";
                }
                switch (currentLine[pos])
                {
                    case '#':
                        if (pos == currentLine.Length - 1 || currentLine[pos + 1] == '#' || currentLine[pos + 1] == ' ')
                        {
                            // comment
                            if (comments != null)
                                comments.Append(currentLine.Substring(pos + 1));
                            pos = currentLine.Length;
                        }
                        else
                        {
                            pos++;
                            return "#";
                        }
                        break;
                    case ' ':
                    case '\t':
                        pos++;
                        break;
                    case '<': // can be operator or keycode
                        if (IsIdentifierChar(currentLine[pos + 1]))
                        {
                            for (int i = pos + 1; i < currentLine.Length; i++)
                            {
                                if (currentLine[i] == '>')
                                {
                                    string result = "K" + currentLine.Substring(pos + 1, i - pos - 1);
                                    pos = i + 1;
                                    return result;
                                }
                                if (currentLine[i] == ' ') break;
                            }
                        }
                        goto default;
                    case '"':
                        String literal = "S";
                        for (int i = pos + 1; i < currentLine.Length; i++)
                        {
                            if (currentLine[i] == '"')
                            {
                                literal += currentLine.Substring(pos + 1, i - pos - 1);
                                pos = i + 1;
                                if (pos == currentLine.Length || currentLine[pos] != '"') return literal;
                                literal += "\"";
                                i = pos;
                            }
                        }
                        throw new ParserException(lineNumber, startPos, "Unclosed string literal.");
                    case '_':
                        if (Regex.IsMatch(currentLine.Substring(pos + 1), "^[ \t]*(# .*|##.*)?$"))
                        {
                            if (comments != null)
                            {
                                string rest = currentLine.Substring(pos + 1);
                                comments.Append(rest.Substring(rest.IndexOf("#") + 1));
                            }
                            currentLine = input.ReadLine();
                            if (currentLine == null)
                                throw new ContinuationException(lineNumber, startPos);
                            baseLineNumber++;
                            pos = 0;
                            break;
                        }
                        goto default;
                    default:
                        if (currentLine[pos] >= '0' && currentLine[pos] <= '9')
                        {
                            // number
                            int start = pos;
                            for (int i = pos + 1; i < currentLine.Length; i++)
                            {
                                if (currentLine[i] < '0' || currentLine[i] > '9')
                                {
                                    pos = i;
                                    return "N" + currentLine.Substring(start, i - start);
                                }
                            }
                            pos = currentLine.Length;
                            return "N" + currentLine.Substring(start);
                        }
                        else if (currentLine[pos] != '_' && IsIdentifierChar(currentLine[pos]))
                        {
                            // identifier
                            int start = pos;
                            for (int i = pos + 1; i < currentLine.Length; i++)
                            {
                                if (!IsIdentifierChar(currentLine[i]))
                                {
                                    pos = i;
                                    return "I" + currentLine.Substring(start, i - start).ToLowerInvariant();
                                }
                            }
                            pos = currentLine.Length;
                            return "I" + currentLine.Substring(start).ToLowerInvariant();
                        }
                        else
                        {
                            foreach (string t in symbolTokens)
                            {
                                if (currentLine.Substring(pos).StartsWith(t))
                                {
                                    pos += t.Length;
                                    return t;
                                }
                            }
                        }
                        throw new ParserException(lineNumber, startPos, "Unknown characters: " + currentLine.Substring(pos));
                }
            }
        }

        private bool IsIdentifierChar(char ch)
        {
            if (ch >= 'A' && ch <= 'Z') return true;
            if (ch >= 'a' && ch <= 'z') return true;
            if (ch >= '0' && ch <= '9') return true;
            if (ch == '_') return true;
            if (ch >= 128) return true;
            return false;
        }
    }
}
