using System;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// This exception is thrown by macro commands if an error occurred. When this
    /// exception is thrown, the macro will be aborted and a message box will be
    /// displayed.
    /// </summary>
    public class MacroErrorException : Exception
    {
        /// <summary>
        /// Creates a new instance of this exception.
        /// </summary>
        /// <param name="message">The message to display in the message box.</param>
        public MacroErrorException(string message) : base(message) { }
    }

    /// <summary>
    /// Throw this exception to abort execution of a macro without showing
    /// a message box.
    /// </summary>
    public class StopMacroException : MacroErrorException
    {
        /// <summary>
        /// Creates a new instance of this exception.
        /// </summary>
        public StopMacroException() : base("Macro stopped.") { }
    }
}
