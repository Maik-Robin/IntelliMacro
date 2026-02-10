using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace IntelliMacro.Runtime.Paths
{
    class PathPattern
    {
        readonly Regex regex;
        readonly string pattern;
        List<NumericRange> ranges = new List<NumericRange>();
        int restOffset = -1;

        public PathPattern(string pattern) : this(pattern, -1, false) { }

        internal PathPattern(string pattern, int startOffset, bool stopAtEquals)
        {
            StringBuilder currentRegex = new StringBuilder();
            if (startOffset == -1)
                startOffset = 0;
            else
                restOffset = pattern.Length;
            for (int i = startOffset; i < pattern.Length; i++)
            {
                switch (pattern[i])
                {
                    case '=':
                        if (stopAtEquals)
                            goto case '&';
                        else
                            goto default;
                    case '&':
                    case '|':
                        if (restOffset != -1)
                        {
                            restOffset = i;
                            pattern = pattern.Substring(0, i - 1);
                            break;
                        }
                        throw new MacroErrorException("Invalid pattern");
                    case '^':
                        if (i == pattern.Length - 1)
                            throw new MacroErrorException("Invalid pattern");
                        i++;
                        goto default;
                    case '*':
                        currentRegex.Append(".*");
                        break;
                    case '{':
                        int depth = 1;
                        int start = i;
                        while (depth > 0)
                        {
                            i++;
                            if (i >= pattern.Length)
                                throw new MacroErrorException("Unbalanced braces");
                            if (pattern[i] == '{') depth++;
                            else if (pattern[i] == '}') depth--;
                            else if (pattern[i] == '^') i++;
                        }
                        string subRegex = pattern.Substring(start + 1, i - start - 1);
                        if (Regex.IsMatch(subRegex, "#(-[0-9])?[0-9]*\\.\\.(-[0-9])?[0-9]*"))
                        {
                            string[] range = subRegex.Substring(1).Split(new string[] { ".." }, StringSplitOptions.None);
                            if (range.Length != 2) throw new Exception();
                            subRegex = "(?<NumericRange" + ranges.Count + ">-?[0-9]+)";
                            ranges.Add(new NumericRange(range[0] == "" ? int.MinValue : int.Parse(range[0]),
                                range[1] == "" ? int.MaxValue : int.Parse(range[1])));
                        }
                        currentRegex.Append(subRegex);
                        break;
                    case '}':
                        throw new MacroErrorException("Unbalanced braces");
                    default:
                        currentRegex.Append(RegexQuote(pattern[i]));
                        break;
                }
            }
            currentRegex.Append("");
            this.pattern = currentRegex.ToString();
            regex = new Regex("^(" + currentRegex.ToString() + ")$");
        }

        internal int RestOffset { get { return restOffset; } }
        internal string Pattern { get { return pattern; } }

        internal bool IsMatch(string text)
        {
            Match m = regex.Match(text);
            if (!m.Success) return false;
            for (int i = 0; i < ranges.Count; i++)
            {
                if (!ranges[i].Contains(int.Parse(m.Groups["NumericRange" + i].Value)))
                    return false;
            }
            return true;
        }

        private static string RegexQuote(char p)
        {
            if (@"$()*+?[]{}\^".Contains("" + p)) return @"\" + p; else return "" + p;
        }

        public static string Quote(string value)
        {
            StringBuilder result = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                if ("*&|{}^".Contains("" + value[i]))
                    result.Append('^');
                result.Append(value[i]);
            }
            return result.ToString();
        }
    }

    class NumericRange
    {
        int min, max;
        public NumericRange(int min, int max)
        {
            this.min = min;
            this.max = max;
        }

        public bool Contains(int value)
        {
            return value >= min && value <= max;
        }
    }
}
