using System;
using System.Collections.Generic;
using System.Drawing;
using ManagedWinapi.Accessibility;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A class that implements this interface receives events while recording macros
    /// and can create commands from these events. Recording listeners are registered by
    /// calling <see cref="ICommandRegistry.RegisterRecordingListener"/> in the plugin's
    /// Init method.
    /// </summary>
    public interface IRecordingListener
    {
        /// <summary>
        /// The priority of this recording listener. 
        /// Recording listeners with Smaller priorities are called first.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Called when the recording started.
        /// </summary>
        /// <param name="recordingState">Which kind of events should be recorded.</param>
        /// <returns>A recorded event to be added to the event list, or <code>null</code> if no event should be added.</returns>
        RecordedEvent RecordingStarted(IRecordingState recordingState);

        /// <summary>
        /// Called when a mouse event occurred. See the MSDN documentation on lowlevel
        /// mouse hooks for parameter descriptions.
        /// </summary>
        /// <returns>A recorded event to be added to the event list, or <code>null</code> if no event should be added.</returns>
        RecordedEvent MouseEventOccurred(int msg, Point pt, int mouseData, int flags, int time, IntPtr dwExtraInfo);

        /// <summary>
        /// Called when a keyboard event occurred. See the MSDN documentation on lowlevel
        /// keyboard hooks for parameter descriptions.
        /// </summary>
        /// <returns>A recorded event to be added to the event list, or <code>null</code> if no event should be added.</returns>
        RecordedEvent KeyboardEventOccurred(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo);

        /// <summary>
        /// Called when an accessible event occurred. See the MSDN documentation on WinEventProc
        /// for parameter descriptions.
        /// </summary>
        /// <returns>A recorded event to be added to the event list, or <code>null</code> if no event should be added.</returns>
        RecordedEvent AccessibleEventOccurred(AccessibleEventType eventType, SystemAccessibleObject obj, IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime);

        /// <summary>
        /// Called when a macro is stopped. All recording listeners have the chance to postprocess events they created,
        /// for example to simplify multiple events to one event or strip spurious events if detected.
        /// </summary>
        /// <param name="eventQueue"></param>
        void PostProcessEventQueue(List<RecordedEvent> eventQueue);

        /// <summary>
        /// Called for every post-processed event whose listenerType is compatible
        /// to this listener.
        /// </summary>
        /// <param name="evt">The event</param>
        /// <returns>A line of macro code, or <code>null</code> if the event cannot
        /// be expanded by this recording listener.</returns>
        string ExpandEvent(RecordedEvent evt);
    }

    /// <summary>
    /// This interface is implemented by classes that provide a macro recorder
    /// to tell the recording listeners what they should record.
    /// </summary>
    public interface IRecordingState
    {
        /// <summary>
        /// Whether to record window switches.
        /// </summary>
        bool RecordWindowSwitches { get; }

        /// <summary>
        /// What kind of mouse events to record.
        /// </summary>
        MouseRecordingState RecordMouse { get; }

        /// <summary>
        /// Whether to record mouse moves.
        /// </summary>
        bool RecordMouseMoves { get; }
    }

    /// <summary>
    /// An enumeration of possible mouse recording states.
    /// </summary>
    public enum MouseRecordingState
    {
        /// <summary>
        /// Do not record mouse events at all.
        /// </summary>
        NONE,

        /// <summary>
        /// Record absolute mouse positions.
        /// </summary>
        ABSOLUTE,

        /// <summary>
        /// Record positions relative to the upper-left window corner.
        /// </summary>
        RELATIVE_CORNER,

        /// <summary>
        /// Record positions relative to the active window. Scale replaying if the
        /// window size is different.
        /// </summary>
        RELATIVE_WINDOW,

        /// <summary>
        /// Record position relative to the control the mouse pointer is in.
        /// </summary>
        RELATIVE_CONTROL
    }

    /// <summary>
    /// An event recorded by a recording listener that has to be 
    /// preprocessed before being written into a macro.
    /// </summary>
    public sealed class RecordedEvent
    {
        readonly Type listenerType;
        readonly string type;
        readonly object[] parameters;
        readonly int intParam;

        /// <summary>
        /// Create a new recorded event.
        /// </summary>
        /// <param name="listenerType">The type of the listener that can expand the event later.</param>
        /// <param name="type">The type of the event (unique for the listener)</param>
        /// <param name="intParam">An integer parameter</param>
        /// <param name="parameters">A list of additional object parameters</param>
        public RecordedEvent(Type listenerType, string type, int intParam, params object[] parameters)
        {
            if (!typeof(IRecordingListener).IsAssignableFrom(listenerType)) throw new ArgumentException("Listener type is no recording listener", "listenerType");
            this.listenerType = listenerType;
            this.type = type;
            this.intParam = intParam;
            this.parameters = parameters;
        }

        /// <summary>
        /// The type of recording listener that can expand the event.
        /// </summary>
        public Type ListenerType { get { return listenerType; } }

        /// <summary>
        /// The event type.
        /// </summary>
        public string Type { get { return type; } }

        /// <summary>
        /// The integer parameter.
        /// </summary>
        public int IntParam { get { return intParam; } }

        /// <summary>
        /// The additional parameters.
        /// </summary>
        public object[] Parameters { get { return parameters; } }
    }

    /// <summary>
    /// A recording listener base class that does not record any events at all.
    /// Useful to override only those methods that you want to listen to.
    /// </summary>
    public abstract class AbstractRecordingListener : IRecordingListener
    {
        /// <summary>
        /// The current state of recording.
        /// </summary>
        protected IRecordingState recordingState;

        ///
        public abstract int Priority { get; }

        ///
        public virtual RecordedEvent MouseEventOccurred(int msg, Point pt, int mouseData, int flags, int time, IntPtr dwExtraInfo) { return null; }

        ///
        public virtual RecordedEvent KeyboardEventOccurred(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo) { return null; }

        ///
        public virtual RecordedEvent AccessibleEventOccurred(AccessibleEventType eventType, SystemAccessibleObject obj, IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime) { return null; }
        ///
        public virtual void PostProcessEventQueue(List<RecordedEvent> eventQueue) { }
        ///
        public virtual string ExpandEvent(RecordedEvent evt) { return null; }

        ///
        public virtual RecordedEvent RecordingStarted(IRecordingState recordingState)
        {
            this.recordingState = recordingState;
            return null;
        }
    }
}
