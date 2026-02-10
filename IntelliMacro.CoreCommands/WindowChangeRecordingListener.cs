using System;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class WindowChangeRecordingListener : AbstractRecordingListener
    {
        public override int Priority
        {
            get { return -1000; }
        }

        WindowNode lastForegroundWindow = null;

        public override RecordedEvent RecordingStarted(IRecordingState recordingState)
        {
            base.RecordingStarted(recordingState);
            lastForegroundWindow = null;
            return RecordCurrentWindow();
        }

        public override RecordedEvent AccessibleEventOccurred(ManagedWinapi.Accessibility.AccessibleEventType eventType, ManagedWinapi.Accessibility.SystemAccessibleObject obj, IntPtr hwnd, uint idObject, uint idChild, uint dwEventThread, uint dwmsEventTime)
        {
            return RecordCurrentWindow();
        }

        public override RecordedEvent KeyboardEventOccurred(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo)
        {
            return RecordCurrentWindow();
        }

        public override RecordedEvent MouseEventOccurred(int msg, System.Drawing.Point pt, int mouseData, int flags, int time, IntPtr dwExtraInfo)
        {
            return RecordCurrentWindow();
        }

        public override string ExpandEvent(RecordedEvent evt)
        {
            if (evt.Type == "Window")
            {
                return "Window \"" + ((string)evt.Parameters[0]).Replace("\"", "\"\"") + "\"";
            }
            return null;
        }

        private RecordedEvent RecordCurrentWindow()
        {
            if (recordingState.RecordWindowSwitches)
            {
                WindowNode lastLastForegroundWindow = lastForegroundWindow;
                lastForegroundWindow = new WindowNode(SystemWindow.ForegroundWindow);

                if (lastForegroundWindow.Equals(lastLastForegroundWindow)) return null;

                // check if it is a MDI window
                SystemWindow[] mdiChildren = lastForegroundWindow.Window.FilterDescendantWindows(false, delegate(SystemWindow w)
                {
                    return (w.ExtendedStyle & WindowExStyleFlags.MDICHILD) != 0 && w.WindowAbove == null;
                });
                if (mdiChildren.Length > 0)
                {
                    lastForegroundWindow = new WindowNode(mdiChildren[0]);
                }
                if (lastForegroundWindow.Equals(lastLastForegroundWindow)) return null;

                string path = lastForegroundWindow.GetAbsolutePath(new WindowRoot(lastLastForegroundWindow));
                if (path != null)
                    return new RecordedEvent(typeof(WindowChangeRecordingListener), "Window", 0, path);
            }
            return null;
        }
    }
}