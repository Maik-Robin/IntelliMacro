using System;
using System.Collections.Generic;
using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    class BrowseRecordingListener : AbstractRecordingListener
    {
        internal static readonly BrowseRecordingListener Instance = new BrowseRecordingListener();

        List<RecordedEvent> cachedEvents = new List<RecordedEvent>();

        public void AddEvent(string type, params object[] parameters)
        {
            lock (cachedEvents)
            {
                cachedEvents.Add(new RecordedEvent(typeof(BrowseRecordingListener), type, 0, parameters));
            }
        }

        public override RecordedEvent RecordingStarted(IRecordingState recordingState)
        {
            lock (cachedEvents)
            {
                cachedEvents.Clear();
            }
            return base.RecordingStarted(recordingState);
        }

        public override int Priority { get { return 100; } }
        public override RecordedEvent AccessibleEventOccurred(ManagedWinapi.Accessibility.AccessibleEventType eventType, ManagedWinapi.Accessibility.SystemAccessibleObject obj, IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime) { return GetCachedEvents(); }
        public override RecordedEvent KeyboardEventOccurred(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo) { return GetCachedEvents(); }
        public override RecordedEvent MouseEventOccurred(int msg, System.Drawing.Point pt, int mouseData, int flags, int time, IntPtr dwExtraInfo) { return GetCachedEvents(); }


        public override void PostProcessEventQueue(List<RecordedEvent> eventQueue)
        {
            lock (cachedEvents)
            {
                eventQueue.AddRange(cachedEvents);
                cachedEvents.Clear();
            }
        }

        private RecordedEvent GetCachedEvents()
        {
            RecordedEvent result;
            lock (cachedEvents)
            {
                if (cachedEvents.Count == 0)
                    result = null;
                else if (cachedEvents.Count == 1)
                    result = cachedEvents[0];
                else
                {
                    result = new RecordedEvent(typeof(BrowseRecordingListener), "BrowseMultiEvent", 0, (object[])cachedEvents.ToArray());
                }
                cachedEvents.Clear();
            }
            return result;
        }

        public override string ExpandEvent(RecordedEvent evt)
        {
            switch (evt.Type)
            {
                case "BrowseMultiEvent":
                    String result = "";
                    foreach (RecordedEvent e in evt.Parameters)
                    {
                        string expanded = ExpandEvent(e);
                        if (expanded != null)
                        {
                            if (result != "") result += "\r\n";
                            result += expanded;
                        }
                    }
                    return result;
                case "BrowseFollowLink":
                    return "Browse \"" + evt.Parameters[0].ToString().Replace("\"", "\"\"") + "\"";
                case "BrowseNavigate":
                    return "BrowseNavigate \"" + evt.Parameters[0].ToString().Replace("\"", "\"\"") + "\"";
                case "BrowseFormElement":
                    string value = "";
                    if (evt.Parameters[2] != null)
                    {
                        value = ", \"" + evt.Parameters[2].ToString().Replace("\"", "\"\"") + "\"";
                    }
                    return "BrowseFormElement \"" + evt.Parameters[0].ToString().Replace("\"", "\"\"") + "\", \"" + evt.Parameters[1].ToString().Replace("\"", "\"\"") + "\"" + value;
                default:
                    return null;
            }
        }
    }
}
