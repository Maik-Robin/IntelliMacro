using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// An abstract base class for objects that can be stored
    /// in macro variables.
    /// </summary>
    [Serializable]
    public abstract class MacroObject : System.Collections.IEnumerable
    {
        /// <summary>
        /// The number 0.
        /// </summary>
        public static MacroObject ZERO = 0;

        /// <summary>
        /// The number 1.
        /// </summary>
        public static MacroObject ONE = 1;

        /// <summary>
        /// The empty string.
        /// </summary>
        public static MacroObject EMPTY = "";

        /// <summary>
        /// Create a macro object from a virtual key name.
        /// </summary>
        public static MacroObject FromKey(string keyName)
        {
            return KeyNames.GetCode(keyName);
        }

        /// <summary>
        /// Create a macro object by deserializing Object Notation.
        /// </summary>
        public static MacroObject FromObjectNotation(string objectNotation)
        {
            int pos = 0;
            MacroObject result = ParseExpression(objectNotation, ref pos);
            SkipWhitespace(objectNotation, ref pos);
            if (pos != objectNotation.Length)
                throw new MacroErrorException("Object Notation cannot be parsed: End of object expected.");
            return result;
        }

        private static void SkipWhitespace(string expression, ref int pos)
        {
            ScanForward(expression, " \t\r\n_", true, ref pos);
        }

        private static void ScanForward(string expression, string toScan, bool negate, ref int pos)
        {
            while (pos < expression.Length && toScan.Contains(expression.Substring(pos, 1)) == negate)
                pos++;
        }

        private static MacroObject ParseExpression(string expression, ref int pos)
        {
            SkipWhitespace(expression, ref pos);
            if (pos == expression.Length)
                throw new MacroErrorException("Object Notation cannot be parsed: Expression missing.");
            switch (expression[pos])
            {
                case '[': // list
                    pos++;
                    List<MacroObject> listItems = new List<MacroObject>();
                    if (pos < expression.Length && expression[pos] == ']')
                    {
                        pos++;
                        return new MacroList(listItems);
                    }
                    while (true)
                    {
                        listItems.Add(ParseExpression(expression, ref pos));
                        SkipWhitespace(expression, ref pos);
                        if (pos == expression.Length)
                            throw new MacroErrorException("Object Notation cannot be parsed: List not closed.");
                        pos++;
                        if (expression[pos - 1] == ']')
                        {
                            return new MacroList(listItems);
                        }
                        else if (expression[pos - 1] != ',')
                        {
                            throw new MacroErrorException("Object Notation cannot be parsed: Invalid list item separator.");
                        }
                    }
                case '#': // string
                case '"':
                    StringBuilder sb = new StringBuilder();
                    AppendString(expression, ref pos, sb);
                    SkipWhitespace(expression, ref pos);
                    while (pos < expression.Length && expression[pos] == '&')
                    {
                        if (pos + 1 < expression.Length && expression[pos + 1] == '&')
                        {
                            pos++;
                            sb.Append(Environment.NewLine);
                        }
                        pos++;
                        SkipWhitespace(expression, ref pos);
                        if (pos == expression.Length)
                            throw new MacroErrorException("Object Notation cannot be parsed: Expression missing.");
                        AppendString(expression, ref pos, sb);
                        SkipWhitespace(expression, ref pos);
                    }
                    return sb.ToString();
                case ':': // wrapped object (:Object:type:base64:)
                    pos++;
                    ScanForward(expression, ":", false, ref pos);
                    pos++;
                    ScanForward(expression, ":", false, ref pos);
                    pos++;
                    int start1 = pos;
                    ScanForward(expression, ":", false, ref pos);
                    if (pos == start1)
                        return new MacroWrappedObject(null);
                    else if (expression.Substring(start1, pos - start1) == "?")
                        throw new MacroErrorException("Object Notation cannot be parsed: Wrapped object not serializable.");
                    MemoryStream ms = new MemoryStream(Convert.FromBase64String(expression.Substring(start1, pos - start1)));
                    pos++;
                    return new MacroWrappedObject(new BinaryFormatter().Deserialize(ms));
                default: // number
                    int start2 = pos;
                    ScanForward(expression, "-0123456789", true, ref pos);
                    long number = -1;
                    if (pos != start2 && long.TryParse(expression.Substring(start2, pos - start2), out number))
                        return number;
                    else
                        throw new MacroErrorException("Object Notation cannot be parsed: Invalid value.");
            }
        }

        private static void AppendString(string expression, ref int pos, StringBuilder sb)
        {
            if (expression[pos] == '#')
            {
                pos++;
                MacroObject num = ParseExpression(expression, ref pos);
                if (!num.IsNumber)
                    throw new MacroErrorException("Object Notation cannot be parsed: Invalid value.");
                sb.Append((char)num.Number);
            }
            else if (expression[pos] == '"')
            {
                pos++;
                int start = pos;
                ScanForward(expression, "\"", false, ref pos);
                if (pos == expression.Length)
                    throw new MacroErrorException("Object Notation cannot be parsed: String not closed.");
                sb.Append(expression.Substring(start, pos - start));
                pos++;
                if (pos < expression.Length && expression[pos] == '"')
                {
                    sb.Append('"');
                    AppendString(expression, ref pos, sb);
                }
            }
            else
            {
                throw new MacroErrorException("Object Notation cannot be parsed: Invalid value.");
            }
        }

        // Do not allow external subclasses
        internal MacroObject() { }

        /// <summary>
        /// Whether this MacroObject is a number.
        /// </summary>
        public bool IsNumber { get { return GetObjectType() == 0; } }

        /// <summary>
        /// The numeric value of this MacroObject.
        /// </summary>
        public long Number { get { return GetNumber(); } }

        /// <summary>
        /// The string value of this MacroObject.
        /// </summary>
        public string String { get { return GetString(); } }

        /// <summary>
        /// The length of this macro object, used for subitems and slices.
        /// </summary>
        public int Length { get { return GetLength(); } }

        /// <summary>
        /// The type of this macro object: 0 for number, 1 for string, 2 for list, 3 for object
        /// </summary>
        public int Type { get { return GetObjectType(); } }

        internal abstract long GetNumber();
        internal abstract string GetString();
        internal abstract int GetLength();
        internal abstract int GetObjectType();
        internal abstract MacroObject this[int i] { get; }
        internal abstract MacroObject GetSlice(int from, int to);
        internal abstract MacroObject SetItem(int index, MacroObject value);
        internal abstract MacroObject SetSlice(int from, int to, MacroObject value);

        /// <summary>
        /// A subitem of this macro object.
        /// </summary>
        public MacroObject this[MacroObject index]
        {
            get
            {
                long i = index.Number;
                if (i == 0) return this;
                if (i < 0)
                {
                    i += Length;
                }
                else
                {
                    i--;
                }
                if (i < 0 || i >= Length) return ZERO;
                return this[(int)i];
            }
        }

        /// <summary>
        /// Returns a slice of this macro object.
        /// </summary>
        public MacroObject GetSlice(MacroObject fromObj, MacroObject toObj)
        {
            long from = fromObj.Number;
            long to = toObj.Number;
            int l = Length;

            if (from < 0) from += l + 1;
            if (to < 0) to += l + 1;
            from--;

            if (from < 0) from = 0;
            if (from > l) from = l;
            if (to < from) to = from;
            if (to > l) to = l;
            return GetSlice((int)from, (int)to);
        }

        /// <summary>
        /// Changes a subitem of this macro object.
        /// </summary>
        /// <returns>The changed copy.</returns>
        public MacroObject SetItem(MacroObject index, MacroObject value)
        {
            long i = index.Number;
            if (i == 0) return this;
            if (i < 0)
            {
                i += Length;
            }
            else
            {
                i--;
            }
            if (i < 0 || i >= Length) return this;
            return SetItem((int)i, value);
        }

        /// <summary>
        /// Changes a slice of this macro object.
        /// </summary>
        /// <returns>The changed copy.</returns>
        public MacroObject SetSlice(MacroObject fromObj, MacroObject toObj, MacroObject value)
        {
            long from = fromObj.Number;
            long to = toObj.Number;
            int l = Length;

            if (from < 0) from += l;
            else if (from > 0) from--;
            if (to == 0) to = l;
            else if (to < 0) to += l + 1;

            if (from < 0) from = 0;
            if (from > l) from = l;
            if (to < from) to = from;
            if (to > l) to = l;
            return SetSlice((int)from, (int)to, value);
        }

        /// <summary>
        /// Adds two macro objects numerically.
        /// </summary>
        public static MacroObject operator +(MacroObject o1, MacroObject o2)
        {
            return o1.Number + o2.Number;
        }

        /// <summary>
        /// Subtracts two macro objects numerically.
        /// </summary>
        public static MacroObject operator -(MacroObject o1, MacroObject o2)
        {
            return o1.Number - o2.Number;
        }

        /// <summary>
        /// Multiplies two macro objects numerically.
        /// </summary>
        public static MacroObject operator *(MacroObject o1, MacroObject o2)
        {
            if (o2 is MacroList)
                return ((MacroList)o2).Repeat(o1.Number);
            return o1.Number * o2.Number;
        }

        /// <summary>
        /// Divides two macro objects numerically.
        /// </summary>
        public static MacroObject operator /(MacroObject o1, MacroObject o2)
        {
            return o1.Number / o2.Number;
        }

        /// <summary>
        /// Computes the remainder of two macro objects numerically.
        /// </summary>
        public static MacroObject operator %(MacroObject o1, MacroObject o2)
        {
            return o1.Number % o2.Number;
        }

        /// <summary>
        /// Negates a macro object numerically.
        /// </summary>
        public static MacroObject operator -(MacroObject o)
        {
            return -o.Number;
        }

        /// <summary>
        /// Raises a macro object to a power.
        /// </summary>
        public MacroObject Pow(MacroObject other)
        {
            return (MacroObject)(long)Math.Pow(this.Number, other.Number);
        }

        /// <summary>
        /// Concatenates two macro objects as strings.
        /// </summary>
        public virtual MacroObject Concat(MacroObject other)
        {
            return String + other.String;
        }

        /// <summary>
        /// Concatenates two macro objects and adds a newline in between.
        /// </summary>
        public MacroObject ConcatNL(MacroObject other)
        {
            return String + Environment.NewLine + other.String;
        }

        /// <summary>
        /// Creates a string consisting of one character specified by
        /// the number of this macro object. For example, MakeCharacter()
        /// invoked on the number 32 will result in a space character.
        /// </summary>
        public MacroObject MakeCharacter()
        {
            return "" + (char)Number;
        }

        /// <summary>
        /// Negates this macro object logically, i. e. return 1 if zero
        /// and 0 otherwise.
        /// </summary>
        public MacroObject Negate()
        {
            if (Number == 0)
                return MacroObject.ONE;
            else
                return MacroObject.ZERO;
        }


        /// <summary>
        /// Convert a string to a macro object.
        /// </summary>
        public static implicit operator MacroObject(string s)
        {
            return new MacroString(s);
        }

        /// <summary>
        /// Convert a number to a macro object.
        /// </summary>
        public static implicit operator MacroObject(long n)
        {
            return new MacroNumber(n);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MacroObjectEnumerator(this);
        }

        /// <summary>
        /// Convert this macro object to Object Notation - a string that can be parsed back to an equal MacroObject.
        /// </summary>
        /// <param name="prettyPrinted">Whether the result should use extra whitespace to make it more readable</param>
        public abstract string ToObjectNotation(bool prettyPrinted);
    }

    class MacroObjectEnumerator : System.Collections.IEnumerator
    {
        readonly MacroObject list;
        int index = -1;

        public MacroObjectEnumerator(MacroObject list) { this.list = list; }

        public void Reset() { index = -1; }
        public bool MoveNext() { index++; return index < list.Length; }

        public object Current
        {
            get
            {
                if (index < 0 || index >= list.Length)
                    throw new InvalidOperationException();
                return list[index];
            }
        }
    }

    /// <summary>
    /// An immutable list of macro objects.
    /// </summary>
    [Serializable]
    public class MacroList : MacroObject
    {
        List<MacroObject> values = new List<MacroObject>();

        /// <summary>
        /// Create a new macro list
        /// </summary>
        /// <param name="values">The values in this list.</param>
        public MacroList(IEnumerable<MacroObject> values)
        {
            foreach (MacroObject o in values)
            {
                this.values.Add(o);
            }
        }

        internal override int GetObjectType() { return 2; }
        internal override int GetLength() { return values.Count; }
        internal override long GetNumber() { return 0; }

        internal override string GetString()
        {
            StringBuilder sb = new StringBuilder("{List\r\n");
            foreach (MacroObject o in values)
            {
                sb.Append("  " + o.String.Replace("\n", "\n    ") + "\r\n");
            }
            sb.Append("}");
            return sb.ToString();
        }

        internal override MacroObject this[int i]
        {
            get { return values[i]; }
        }

        internal override MacroObject GetSlice(int from, int to)
        {
            IList<MacroObject> sliceList = new List<MacroObject>();
            for (int i = from; i < to; i++)
            {
                sliceList.Add(values[i]);
            }
            return new MacroList(sliceList);
        }

        internal override MacroObject SetItem(int index, MacroObject value)
        {
            List<MacroObject> newList = new List<MacroObject>(values);
            newList[index] = value;
            return new MacroList(newList);
        }

        internal override MacroObject SetSlice(int from, int to, MacroObject value)
        {
            List<MacroObject> newList = new List<MacroObject>(values);
            newList.RemoveRange(from, to - from);
            MacroObject[] newElems = new MacroObject[value.Length];
            for (int i = 0; i < newElems.Length; i++)
            {
                newElems[i] = value[i];
            }
            newList.InsertRange(from, newElems);
            return new MacroList(newList);
        }

        ///
        public override MacroObject Concat(MacroObject other)
        {
            if (other is MacroList)
            {
                List<MacroObject> result = new List<MacroObject>();
                result.AddRange(values);
                result.AddRange(((MacroList)other).values);
                return new MacroList(result);
            }
            else
            {
                return base.Concat(other);
            }
        }

        internal MacroObject Repeat(long times)
        {
            List<MacroObject> result = new List<MacroObject>();
            for (int i = 0; i < Math.Abs(times); i++)
            {
                for (int j = 0; j < values.Count; j++)
                {
                    result.Add(values[times < 0 ? values.Count - 1 - j : j]);
                }
            }
            return new MacroList(result);
        }

        ///
        public override string ToObjectNotation(bool prettyPrinted)
        {
            StringBuilder sb = new StringBuilder("[");
            bool first = true;
            foreach (MacroObject o in values)
            {
                if (!first)
                {
                    sb.Append(',');
                }
                if (prettyPrinted) sb.Append(" _\r\n");
                first = false;
                if (prettyPrinted)
                    sb.Append("  ");
                string str = o.ToObjectNotation(prettyPrinted);
                if (prettyPrinted)
                    str = str.Replace("\n", "\n  ");
                sb.Append(str);
            }
            if (prettyPrinted && !first)
                sb.Append(" _\r\n");
            sb.Append("]");
            return sb.ToString();
        }
    }

    [Serializable]
    class MacroString : MacroObject
    {
        string value;

        internal MacroString(string value)
        {
            if (value == null) throw new ArgumentNullException();
            this.value = value;
        }

        internal override int GetObjectType() { return 1; }
        internal override string GetString() { return value; }
        internal override int GetLength() { return value.Length; }

        internal override long GetNumber()
        {
            long result;
            if (!long.TryParse(value, out result))
                result = 0;
            return result;
        }

        internal override MacroObject this[int i]
        {
            get { return value[i]; }
        }

        internal override MacroObject GetSlice(int from, int to)
        {
            return value.Substring(from, to - from);
        }

        internal override MacroObject SetItem(int index, MacroObject newValue)
        {
            return value.Substring(0, index) + ((char)newValue.Number) + value.Substring(index + 1);
        }

        internal override MacroObject SetSlice(int from, int to, MacroObject newValue)
        {
            return value.Substring(0, from) + newValue.String + value.Substring(to);
        }

        public override string ToObjectNotation(bool prettyPrinted)
        {
            const int STATE_BEFORE = 0, STATE_INSIDE = 1, STATE_AFTER = 2;
            int state = STATE_BEFORE;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < value.Length; i++)
            {
                if (value.Substring(i).StartsWith(Environment.NewLine))
                {
                    if (state == STATE_BEFORE)
                        sb.Append('"');
                    if (state != STATE_AFTER)
                        sb.Append('"');
                    sb.Append(prettyPrinted ? " && _\r\n" : "&&");
                    state = STATE_BEFORE;
                    i += Environment.NewLine.Length - 1;
                }
                else if (value[i] < 32 || value[i] > 127)
                {
                    if (state == STATE_INSIDE)
                        sb.Append('"');
                    if (state != STATE_BEFORE)
                        sb.Append(prettyPrinted ? " & " : "&");
                    sb.Append("#" + (int)value[i]);
                    state = STATE_AFTER;
                }
                else
                {
                    if (state == STATE_AFTER)
                        sb.Append(prettyPrinted ? " & " : "&");
                    if (state != STATE_INSIDE)
                        sb.Append('"');
                    state = STATE_INSIDE;
                    sb.Append(value[i] == '"' ? "\"" : "").Append(value[i]);
                }
            }
            if (state == STATE_BEFORE)
                sb.Append('"');
            if (state != STATE_AFTER)
                sb.Append('"');
            return sb.ToString();
        }
    }

    [Serializable]
    class MacroNumber : MacroString
    {
        long value;

        public MacroNumber(long value)
            : base("" + value)
        {
            this.value = value;
        }

        internal override long GetNumber() { return value; }
        internal override int GetObjectType() { return 0; }

        ///
        public override string ToObjectNotation(bool prettyPrinted)
        {
            return GetString();
        }
    }

    /// <summary>
    /// A macro object that wraps any other object which can later
    /// be unwrapped.
    /// </summary>
    [Serializable]
    public class MacroWrappedObject : MacroObject
    {
        readonly object wrapped;

        /// <summary>
        /// Creates a new wrapped object
        /// </summary>
        public MacroWrappedObject(object wrapped)
        {
            if (wrapped == null) throw new ArgumentException();
            this.wrapped = wrapped;
        }

        /// <summary>
        /// The object wrapped in this wrapped object.
        /// </summary>
        public object Wrapped { get { return wrapped; } }

        internal override string GetString()
        {
            return "{Object: " + wrapped.GetType().Name + "}";
        }

        internal override int GetObjectType() { return 3; }
        internal override long GetNumber() { return 0; }
        internal override int GetLength() { return 0; }
        internal override MacroObject this[int i] { get { return this; } }
        internal override MacroObject GetSlice(int from, int to) { return this; }
        internal override MacroObject SetItem(int index, MacroObject value) { return this; }
        internal override MacroObject SetSlice(int from, int to, MacroObject value) { return this; }

        /// <summary>
        /// Unwrap the given object. If the object is not a wrapped object,
        /// it is returned as is.
        /// </summary>
        public static object Unwrap(MacroObject macroObject)
        {
            if (macroObject == null)
                return MacroObject.EMPTY;
            else if (macroObject is MacroWrappedObject)
                return ((MacroWrappedObject)macroObject).Wrapped;
            else
                return macroObject;
        }

        ///
        public override string ToObjectNotation(bool prettyPrinted)
        {
            string base64Data;
            try
            {
                MemoryStream ms = new MemoryStream();
                new BinaryFormatter().Serialize(ms, wrapped);
                base64Data = Convert.ToBase64String(ms.GetBuffer());
            }
            catch
            {
                base64Data = "?";
            }
            return ":Object:" + wrapped.GetType().Name + ":" + base64Data + ":";
        }
    }
}