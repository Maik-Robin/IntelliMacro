using System;
using System.Collections.Generic;
using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    class FindCommand : AbstractCommand
    {
        internal FindCommand() : base("Find", true, "&Find", "&List/String functions") { }

        public override string Description
        {
            get
            {
                return "Find a string/list inside another string/list.\n\n" +
                    "By default, return the index of the first match.\n" +
                    "If the third argument is negative, count the matches from the last one, if positive from the first one.\n" +
                    "If the third argument is zero, return a list of all matches.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "haystack"),
                    new ParameterDescription(false, "needle"),
                    new ParameterDescription(true, "whichMatch"),
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            IList<int> positions = FindAll(parameters[0], parameters[1]);
            long which = parameters[2] == null ? 1 : parameters[2].Number;
            if (which > 0)
            {
                if (which > positions.Count) return MacroObject.ZERO;
                return positions[(int)which - 1] + 1;
            }
            else if (which < 0)
            {
                if (which < -positions.Count) return MacroObject.ZERO;
                return positions[positions.Count + (int)which] + 1;
            }
            else
            {
                MacroObject[] objs = new MacroObject[positions.Count];
                for (int i = 0; i < objs.Length; i++)
                {
                    objs[i] = positions[i] + 1;
                }
                return new MacroList(objs);
            }
        }

        private IList<int> FindAll(MacroObject haystack, MacroObject needle)
        {
            if (haystack is MacroList)
            {
                if (needle is MacroList)
                {
                    List<int> positions = new List<int>();
                    for (int i = 0; i <= haystack.Length - needle.Length; i++)
                    {
                        bool match = true;
                        for (int j = 1; j <= needle.Length; j++)
                        {
                            if (haystack[i + j].String != needle[j].String)
                            {
                                match = false;
                                break;
                            }
                        }
                        if (match) positions.Add(i);
                    }
                    return positions;
                }
                else
                {
                    return new int[0];
                }
            }
            else
            {
                List<int> positions = new List<int>();
                int pos = haystack.String.IndexOf(needle.String);
                while (pos != -1)
                {
                    positions.Add(pos);
                    pos = haystack.String.IndexOf(needle.String, pos + 1);
                }
                return positions;
            }
        }
    }

    class SortCommand : AbstractCommand
    {

        public SortCommand() : base("Sort", true, "S&ort list", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "list"),
                    new ParameterDescription(true, "direction")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Sort a list.\n" +
                    "direction can be '+' for ascending or '-' for descending.\n" +
                    "For numerical sorting, use 'NUM+' and 'NUM-'.\n" +
                    "For case insensitive sorting use 'TXT+' and 'TXT-'.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            MacroObject[] list = new MacroObject[parameters[0].Length];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = parameters[0][i + 1];
            }

            Comparison<MacroObject> comparison;
            switch (parameters[1] == null ? "+" : parameters[1].String)
            {
                case "+": comparison = (x, y) => x.String.CompareTo(y.String); break;
                case "-": comparison = (x, y) => y.String.CompareTo(x.String); break;
                case "NUM+": comparison = (x, y) => x.Number.CompareTo(y.Number); break;
                case "NUM-": comparison = (x, y) => y.Number.CompareTo(x.Number); break;
                case "TXT+": comparison = (x, y) => x.String.ToUpperInvariant().CompareTo(y.String.ToUpperInvariant()); break;
                case "TXT-": comparison = (x, y) => y.String.ToUpperInvariant().CompareTo(x.String.ToUpperInvariant()); break;

                default:
                    throw new MacroErrorException("Unknown sort order: " + parameters[1].String);
            }

            Array.Sort(list, comparison);

            return new MacroList(list);
        }
    }
}
