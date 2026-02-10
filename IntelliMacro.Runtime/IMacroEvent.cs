using System;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// An event that can be listened to by the scheduler.
    /// </summary>
    public interface IMacroEvent : IDisposable
    {
        /// <summary>
        /// Fires when the event has occurred.
        /// </summary>
        event EventHandler Occurred;

        /// <summary>
        /// Get whether the event has occurred since <see cref="ClearOccurred"/> was called last.
        /// Use this for polling an event.
        /// </summary>
        bool HasOccurred { get; }

        /// <summary>
        /// Reset the <see cref="HasOccurred"/> flag.
        /// </summary>
        void ClearOccurred();
    }

    /// <summary>
    /// A helper class that facilitates writing macro events.
    /// </summary>
    public abstract class AbstractMacroEvent : IMacroEvent
    {
        /// <summary>
        /// Fires when the event has occurred.
        /// </summary>
        public event EventHandler Occurred;

        bool hasOccurred;

        /// <summary>
        /// Get whether the event has occurred since <see cref="ClearOccurred"/> was called last.
        /// Use this for polling an event.
        /// </summary>
        public bool HasOccurred
        {
            get
            {
                return hasOccurred;
            }
        }

        /// <summary>
        /// Reset the <see cref="HasOccurred"/> flag.
        /// </summary>
        public void ClearOccurred()
        {
            hasOccurred = false;
        }

        /// <summary>
        /// Fire the event.
        /// </summary>
        protected void FireEvent()
        {
            hasOccurred = true;
            if (Occurred != null) Occurred(this, EventArgs.Empty);
        }

        /// <summary>
        /// Clean up resources needed for the event.
        /// </summary>
        public abstract void Dispose();
    }
}
