using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class CoreRecordingListener : AbstractRecordingListener
    {

        Point lastMousePoint;
        bool gotKeyDown = false;

        public override int Priority { get { return 0; } }

        public override RecordedEvent RecordingStarted(IRecordingState recordingState)
        {
            gotKeyDown = false;
            lastMousePoint = new Point(-1, -1);
            return base.RecordingStarted(recordingState);
        }

        public override RecordedEvent KeyboardEventOccurred(int msg, int vkCode, int scanCode, int flags, int time, IntPtr dwExtraInfo)
        {
            switch (msg)
            {
                case 256: // WM_KEYDOWN
                case 260: // WM_SYSKEYDOWN
                    gotKeyDown = true;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Down", vkCode);

                case 257: // WM_KEYUP
                case 261: // WM_SYSKEYUP
                    if (!gotKeyDown) return null;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Up", vkCode);
                default:
                    return null;
            }
        }

        public override RecordedEvent MouseEventOccurred(int msg, Point pt, int mouseData, int flags, int time, IntPtr dwExtraInfo)
        {
            MouseRecordingState mrs = recordingState.RecordMouse;
            if (mrs == MouseRecordingState.NONE) return null;
            switch (msg)
            {
                case 512: // WM_MOUSEMOVE
                    if (recordingState.RecordMouseMoves)
                        return new RecordedEvent(typeof(CoreRecordingListener), "MouseMove", 0, GetMousePosition(mrs, pt));
                    return null;
                case 513: // WM_LBUTTONDOWN
                    gotKeyDown = true;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Down", 1, GetMousePosition(mrs, pt));
                case 514: // WM_LBUTTONUP
                    if (!gotKeyDown) return null;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Up", 1, GetMousePosition(mrs, pt));
                case 516: // WM_RBUTTONDOWN
                    gotKeyDown = true;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Down", 2, GetMousePosition(mrs, pt));
                case 517: // WM_RBUTTONUP
                    if (!gotKeyDown) return null;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Up", 2, GetMousePosition(mrs, pt));
                case 519: // WM_MBUTTONDOWN
                    gotKeyDown = true;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Down", 4, GetMousePosition(mrs, pt));
                case 520: // WM_MBUTTONUP
                    if (!gotKeyDown) return null;
                    return new RecordedEvent(typeof(CoreRecordingListener), "Up", 4, GetMousePosition(mrs, pt));

                case 515: // WM_LBUTTONDBLCLK
                case 518: // WM_RBUTTONDBLCLK
                case 521: // WM_MBUTTONDBLCLK
                    return null;
                case 522: // WM_MOUSEWHEEL
                    return new RecordedEvent(typeof(CoreRecordingListener), "MouseWheel", mouseData >> 16, GetMousePosition(mrs, pt));
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown mouse event: " + msg);
                    return null;
            }
        }

        public override void PostProcessEventQueue(List<RecordedEvent> eventQueue)
        {
            int lastKeyDown = -1;
            for (int i = 0; i < eventQueue.Count; i++)
            {
                if (eventQueue[i].ListenerType == typeof(CoreRecordingListener) && eventQueue[i].Type == "Down" && eventQueue[i].Parameters.Length == 0)
                {
                    int lastLastKeyDown = lastKeyDown;
                    lastKeyDown = eventQueue[i].IntParam;
                    if (lastLastKeyDown == lastKeyDown)
                    {
                        switch (lastKeyDown)
                        {
                            case (int)Keys.LShiftKey:
                            case (int)Keys.RShiftKey:
                            case (int)Keys.ShiftKey:
                            case (int)Keys.ControlKey:
                            case (int)Keys.LControlKey:
                            case (int)Keys.RControlKey:
                            case (int)Keys.Menu:
                            case (int)Keys.LMenu:
                            case (int)Keys.RMenu:
                            case (int)Keys.LWin:
                            case (int)Keys.RWin:
                                eventQueue.RemoveAt(i);
                                i--;
                                break;
                            default: break;
                        }
                    }
                }
            }
            for (int i = 0; i < eventQueue.Count - 1; i++)
            {
                if (eventQueue[i].ListenerType == typeof(CoreRecordingListener) && eventQueue[i + 1].ListenerType == typeof(CoreRecordingListener) && eventQueue[i].Type == "Down" && eventQueue[i + 1].Type == "Up")
                {
                    if (eventQueue[i].IntParam == eventQueue[i + 1].IntParam && MouseEventEquals(eventQueue[i].Parameters, eventQueue[i + 1].Parameters))
                    {
                        eventQueue[i] = new RecordedEvent(typeof(CoreRecordingListener), "DownUp", eventQueue[i].IntParam, eventQueue[i].Parameters);
                        eventQueue.RemoveAt(i + 1);
                        if (eventQueue[i].Parameters.Length == 0)
                        {
                            int maxSame = 0;
                            for (int j = 1; j < 5; j++)
                            {
                                if (i - j < 0 || i + j >= eventQueue.Count) break;
                                if (eventQueue[i - j].Type == "Down" && eventQueue[i + j].Type == "Up" && eventQueue[i - j].IntParam == eventQueue[i + j].IntParam && eventQueue[i - j].Parameters.Length == 0 && eventQueue[i + j].Parameters.Length == 0)
                                {
                                    maxSame = j;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            if (maxSame > 0)
                            {
                                object[] chord = new object[maxSame + 1];
                                chord[maxSame] = eventQueue[i].IntParam;
                                for (int j = maxSame - 1; j >= 0; j--)
                                {
                                    chord[j] = eventQueue[i + 1].IntParam;
                                    eventQueue.RemoveAt(i - 1);
                                    i--;
                                    eventQueue.RemoveAt(i + 1);
                                }
                                eventQueue[i] = new RecordedEvent(typeof(CoreRecordingListener), "DownUp", -1, chord);
                            }
                        }
                    }
                }
            }
        }

        private bool MouseEventEquals(object[] p1, object[] p2)
        {
            if (p1.Length == 0 && p2.Length == 0) return true;
            if (p1.Length == 0 || p2.Length == 0) return false;
            return (((Point)p1[0]).Equals(p2[0]));
        }

        public override string ExpandEvent(RecordedEvent evt)
        {

            if (evt.ListenerType == typeof(IRecordingListener) && evt.Type == "Delay")
            {
                // the Delay type is recorded directly in the macro recorder;
                // it is therefore recorded with the IRecordingListner type
            }
            else if (evt.ListenerType != typeof(CoreRecordingListener))
            {
                return null;
            }
            string prefix = "";
            switch (evt.Type)
            {
                case "Down":
                case "Up":
                case "DownUp":
                    if (evt.IntParam == -1)
                    {
                        StringBuilder result = new StringBuilder(evt.Type + " ");
                        for (int i = 0; i < evt.Parameters.Length; i++)
                        {
                            if (i > 0) result.Append(", ");
                            result.Append("<" + KeyNames.GetName((int)evt.Parameters[i]) + ">");
                        }
                        return result.ToString();
                    }
                    if (evt.Parameters.Length > 0 && !lastMousePoint.Equals(evt.Parameters[0]))
                    {
                        prefix = ExpandMouseEvent(evt.Parameters) + "\r\n";
                        lastMousePoint = (Point)evt.Parameters[0];
                    }
                    return prefix + evt.Type + " <" + KeyNames.GetName(evt.IntParam) + ">";
                case "MouseMove":
                    lastMousePoint = (Point)evt.Parameters[0];
                    return ExpandMouseEvent(evt.Parameters);
                case "MouseWheel":
                    if (evt.Parameters.Length > 0 && !lastMousePoint.Equals(evt.Parameters[0]))
                    {
                        prefix = ExpandMouseEvent(evt.Parameters) + "\r\n";
                        lastMousePoint = (Point)evt.Parameters[0];
                    }
                    return prefix + "Wheel " + evt.IntParam;
                case "Delay":
                    return "Delay " + evt.IntParam;
                default:
                    return null;
            }
        }

        private string ExpandMouseEvent(object[] mouseEvt)
        {
            Point pt = (Point)mouseEvt[2];
            string suffix = "";
            if (mouseEvt.Length == 4)
            {
                suffix = ", \"" + ((string)mouseEvt[3]).Replace("\"", "\"\"") + "\"";
            }
            return (string)mouseEvt[1] + " " + pt.X + ", " + pt.Y + suffix;
        }


        private object[] GetMousePosition(MouseRecordingState mrs, Point ptBase)
        {
            if (mrs == MouseRecordingState.ABSOLUTE)
                return new object[] { ptBase, "Mouse", ptBase };
            SystemWindow fg = SystemWindow.ForegroundWindow;
            SystemWindow baseWindow = fg;
            Rectangle rect = baseWindow.Rectangle;
            if (!rect.Contains(ptBase))
                return new object[] { ptBase, "Mouse", ptBase };
            if (mrs == MouseRecordingState.RELATIVE_CONTROL)
            {
                baseWindow = SystemWindow.FromPointEx(ptBase.X, ptBase.Y, false, false);
                SystemWindow tmp = baseWindow;
                while (tmp.ParentSymmetric != null) tmp = tmp.ParentSymmetric;
                if (tmp.HWnd != fg.HWnd)
                    baseWindow = fg;
                rect = baseWindow.Rectangle;
            }
            Point pt = new Point(ptBase.X - rect.Left, ptBase.Y - rect.Top);
            if (mrs == MouseRecordingState.RELATIVE_CORNER)
            {
                return new object[] { ptBase, "MouseRel", pt };
            }
            pt = new Point(pt.X * 1000 / rect.Width, pt.Y * 1000 / rect.Height);
            if (fg.HWnd == baseWindow.HWnd)
            {
                return new Object[] { ptBase, "MouseRelWindow", pt };
            }
            return new object[] { ptBase, "MouseRelCtrl", pt, GetRelativeWindowPath(baseWindow, fg) };
        }

        private string GetRelativeWindowPath(SystemWindow window, SystemWindow ancestor)
        {
            return new WindowNode(window).GetRelativePath(new WindowNode(ancestor));
        }
    }
}
