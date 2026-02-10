using System;

namespace IntelliMacro.Core
{
    public class ParserException : Exception
    {
        int lineNumber, columnNumber;

        public ParserException(int lineNumber, int columnNumber, string message)
            : base(message)
        {
            this.lineNumber = lineNumber;
            this.columnNumber = columnNumber;
        }

        public int LineNumber { get { return lineNumber; } }
        public int ColumnNumber { get { return columnNumber; } }
    }

    class ContinuationException : ParserException
    {
        public ContinuationException(int lineNumber, int columnNumber) : base(lineNumber, columnNumber, "Continuation found in last input line.") { }
    }
}
