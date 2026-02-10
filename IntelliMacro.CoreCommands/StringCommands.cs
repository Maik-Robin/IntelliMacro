using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using IntelliMacro.Runtime;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace IntelliMacro.CoreCommands
{
    abstract class AbstractStringCommand : AbstractCommand
    {
        internal AbstractStringCommand(string name, string displayName) : base(name, true, displayName, "&List/String functions") { }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "inputString")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            return Perform(parameters[0].String);
        }

        protected abstract string Perform(string input);
    }

    class UCaseCommand : AbstractStringCommand
    {
        internal UCaseCommand() : base("UCase", "Convert to &upper case") { }

        protected override string Perform(string input) { return input.ToUpperInvariant(); }

        public override string Description
        {
            get { return "Convert a string to upper case."; }
        }
    }

    class LCaseCommand : AbstractStringCommand
    {
        internal LCaseCommand() : base("LCase", "Convert to &lower case") { }

        protected override string Perform(string input) { return input.ToLowerInvariant(); }

        public override string Description
        {
            get { return "Convert a string to lower case."; }
        }
    }

    class TrimCommand : AbstractCommand
    {
        internal TrimCommand() : base("Trim", true, "&Trim string", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "inputString"),
                    new ParameterDescription(true, "characters"),
                    new ParameterDescription(true, "ends"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Trim whitespace or other characters from the ends of a string.\n" +
                    "If characters is given, trim characters in that string instead.\n" +
                    "If ends is 1, trim only at the start, if ends is 2, trim only at the end.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            string input = parameters[0].String;
            string chars = parameters[1] == null ? "\t\n\v\r " : parameters[1].String;
            if (parameters[2] != null && parameters[2].IsNumber)
            {
                if (parameters[2].Number == 1)
                    input = input.TrimStart(chars.ToCharArray());
                else if (parameters[2].Number == 2)
                    input = input.TrimEnd(chars.ToCharArray());
                else
                    input = input.Trim(chars.ToCharArray());
            }
            else
            {
                input = input.Trim(chars.ToCharArray());
            }
            return input;
        }
    }

    internal class ReplaceCommand : AbstractCommand
    {

        internal ReplaceCommand() : base("Replace", true, "&Replace", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "inputString"),
                    new ParameterDescription(false, "search"),
                    new ParameterDescription(true, "replace"),
                    new ParameterDescription(true, "useRegex"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Replace all occurrences of a given string in another string.\n" +
                    "If replace is not given, split into an array instead. If useRegex is given, use a regex for replace or split.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (parameters[3] != null)
            {
                Regex r = new Regex(parameters[1].String);
                if (parameters[2] != null)
                {
                    return r.Replace(parameters[0].String, parameters[2].String);
                }
                else
                {
                    return new MacroList(Array.ConvertAll(r.Split(parameters[0].String), s => (MacroObject)s));
                }
            }
            else
            {
                if (parameters[2] != null)
                {
                    return parameters[0].String.Replace(parameters[1].String, parameters[2].String);
                }
                else
                {
                    string[] result;
                    if (parameters[1] is MacroList)
                    {
                        MacroList ml = (MacroList)parameters[1];
                        string[] strings = new string[ml.Length];
                        for (long i = 0; i < ml.Length; i++)
                        {
                            strings[i] = ml[i].String;
                        }
                        result = parameters[0].String.Split(strings, StringSplitOptions.None);
                    }
                    else
                    {
                        result = parameters[0].String.Split(new string[] { parameters[1].String }, StringSplitOptions.None);
                    }
                    return new MacroList(Array.ConvertAll(result, s => (MacroObject)s));
                }
            }
        }
    }

    internal class JoinCommand : AbstractCommand
    {

        public JoinCommand() : base("Join", true, "&Join/Combine", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "items"),
                    new ParameterDescription(false, "joiner"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Join strings or arrays in an array and add a separator in between.\n" +
                    "items is an array of strings or arrays, joiner is a string or array.\n" +
                    "If joiner is a string, all items are treated as strings, if it is an array, all items are treated as arrays.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            MacroObject items = parameters[0], joiner = parameters[1];
            if (joiner is MacroList)
            {
                List<MacroObject> resultList = new List<MacroObject>();
                for (int i = 0; i < items.Length; i++)
                {
                    if (i != 0)
                    {
                        for (int j = 0; j < joiner.Length; j++)
                        {
                            resultList.Add(joiner[j + 1]);
                        }
                    }
                    MacroObject item = items[i + 1];
                    for (int j = 0; j < item.Length; j++)
                    {
                        resultList.Add(item[j + 1]);
                    }
                }
                return new MacroList(resultList);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                String joinerString = joiner.String;
                for (int i = 0; i < items.Length; i++)
                {
                    if (i != 0)
                        sb.Append(joinerString);
                    sb.Append(items[i + 1].String);
                }
                return sb.ToString();
            }
        }
    }

    internal class SplitCommand : AbstractCommand
    {

        public SplitCommand() : base("Split", true, "&Split", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] { 
                    new ParameterDescription(false, "input"),
                    new ParameterDescription(false, "separator"),
                    new ParameterDescription(true, "regex"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Split a string into pieces.\n" +
                    "input is the string to split, separator is either a string or a list of strings.\n" +
                    "If useRegex is given, use regular expressions. In this case, all separators are concatenated using | to one regular expression.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            MacroObject separator = parameters[1];
            string[] separators;
            if (separator is MacroList)
            {
                separators = new string[separator.Length];
                for (int i = 0; i < separators.Length; i++)
                {
                    separators[i] = separator[i + 1].String;
                }
            }
            else
            {
                separators = new string[] { separator.String };
            }
            string[] pieces;
            if (parameters[2] != null) // useRegex
            {
                pieces = new Regex(string.Join("|", separators)).Split(parameters[0].String);
            }
            else
            {
                pieces = parameters[0].String.Split(separators, StringSplitOptions.None);
            }
            return new MacroList(Array.ConvertAll<string, MacroObject>(pieces, part => part));
        }
    }

    class FormatCommand : AbstractCommand
    {
        public FormatCommand() : base("Format", true, "Format &values", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "value"),
                    new ParameterDescription(false, "format"),
                    new ParameterDescription(true, "culture")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Format one ore more values based on a format string.\n\n" +
                    "value can be a number, a string, a wrapped object or a list of those.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            CultureInfo culture = null;
            if (parameters[2] != null)
            {
                try
                {
                    if (parameters[2].IsNumber)
                    {
                        culture = new CultureInfo((int)parameters[2].Number);
                    }
                    else
                    {
                        culture = new CultureInfo(parameters[2].String);
                    }
                    if (culture.IsNeutralCulture) culture = null;
                }
                catch (ArgumentException)
                {
                    culture = null;
                }

            }
            try
            {
                if (parameters[0] is MacroList)
                {
                    object[] values = new object[parameters[0].Length];
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = Unwrap(parameters[0][i + 1]);
                    }
                    return string.Format(culture, parameters[1].String, values);
                }
                else
                {
                    object value = Unwrap(parameters[0]);
                    if (value is IFormattable)
                        return ((IFormattable)value).ToString(parameters[1].String, culture);
                    return value.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
        }

        private object Unwrap(MacroObject macroObject)
        {
            if (macroObject is MacroWrappedObject)
            {
                return MacroWrappedObject.Unwrap(macroObject);
            }
            else if (macroObject.IsNumber)
            {
                return macroObject.Number;
            }
            else
            {
                return macroObject.String;
            }
        }
    }

    class SerializeCommand : AbstractCommand
    {
        internal SerializeCommand() : base("Serialize", true, "S&erialize/Deserialize", "&List/String functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "content"),
                    new ParameterDescription(true, "format"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Serialize an object to a string or deserialize the resulting string to an object.\n" +
                    "Format can be \"=\" for binary serialization, or \"=b\" for Base64 binary serialization, or \":\" for object notation, or \"::\" for pretty printed object notation (default).\n" +
                    "To deserialize, add a \"-\" to the beginning of the format.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            MacroObject content = parameters[0];
            string format = parameters[1] == null ? "" : parameters[1].String;
            switch (format.ToLowerInvariant())
            {
                case "=":
                    MemoryStream ms1 = new MemoryStream();
                    new BinaryFormatter().Serialize(ms1, parameters[1]);
                    return Encoding.GetEncoding("ISO-8859-1").GetString(ms1.ToArray());
                case "=b":
                    MemoryStream ms2 = new MemoryStream();
                    new BinaryFormatter().Serialize(ms2, parameters[1]);
                    return Convert.ToBase64String(ms2.ToArray());
                case ":":
                    return content.ToObjectNotation(false);
                case "":
                case "::":
                    return content.ToObjectNotation(true);
                case "-=":
                    MemoryStream ms3 = new MemoryStream(Encoding.GetEncoding("ISO-8859-1").GetBytes(content.String));
                    return (MacroObject)new BinaryFormatter().Deserialize(ms3);
                case "-=b":
                    MemoryStream ms4 = new MemoryStream(Convert.FromBase64String(content.String));
                    return (MacroObject)new BinaryFormatter().Deserialize(ms4);
                case "-":
                case "-:":
                case "-::":
                    return MacroObject.FromObjectNotation(content.String);
                default:
                    throw new MacroErrorException("Unsupported serialization format");
            }
        }
    }
}