using System;
using System.Collections.Generic;
using System.Drawing;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A class that encapsulates the state of a currently running macro.
    /// </summary>
    public class MacroContext
    {
        Dictionary<AdditionalObjectKey, object> additionalObjects = new Dictionary<AdditionalObjectKey, object>();

        /// <summary>
        /// Creates a new macro context.
        /// </summary>
        public MacroContext()
        {
            DelayMultiplier = 1;
            DelayDivisor = 1;
            InputEmulator = PhysicalInputEmulator.Instance;
            Random = new Random();
        }

        /// <summary>
        /// The window that should be focused.
        /// </summary>
        public WindowNode Window { get; set; }

        /// <summary>
        /// The position the mouse should be at the moment, or <c>null</c>
        /// if the mouse may be anywhere.
        /// </summary>
        public Point? MousePosition { get; set; }

        /// <summary>
        /// The currently enabled input blocker, if any.
        /// </summary>
        public InputBlocker InputBlocker { get; set; }

        /// <summary>
        /// The currently active input emulator.
        /// </summary>
        public InputEmulator InputEmulator { get; set; }

        /// <summary>
        /// The random number generator used by some of the commands.
        /// </summary>
        public Random Random { get; private set; }

        /// <summary>
        /// The multiplier all delays should be multiplied with. 
        /// Used by the DelayMult command.
        /// </summary>
        public int DelayMultiplier { get; set; }

        /// <summary>
        /// The divisor all delays should be divided by. 
        /// Used by the DelayMult command.
        /// </summary>
        public int DelayDivisor { get; set; }

        /// <summary>
        /// Return an additional object used by a plugin's command, referenced by a key.
        /// </summary>
        public object GetAdditionalObject(AdditionalObjectKey key)
        {
            return additionalObjects[key];
        }

        /// <summary>
        /// Set an additional object used by a plugin's command, referenced by a key.
        /// </summary>
        public void SetAdditionalObject(AdditionalObjectKey key, object value)
        {
            additionalObjects[key] = value;
        }
    }

    /// <summary>
    /// A key used to reference a specific additional object from a 
    /// <see cref="MacroContext"/>.
    /// </summary>
    public sealed class AdditionalObjectKey { }
}
