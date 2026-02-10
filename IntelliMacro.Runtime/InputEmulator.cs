// TODO move this class over to Managed Winapi when it is stable enough
using System;
using System.Collections.Generic;
using System.Text;
using IntelliMacro.Runtime;
using System.Drawing;
using ManagedWinapi.Windows;
using ManagedWinapi;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// Subclasses of this class are responsible for emulating/replaying input events.
    /// </summary>
    public abstract class InputEmulator
    {
        /// <summary>
        /// Get or set the foreground window.
        /// </summary>
        public abstract SystemWindow ForegroundWindow { get; set; }

        /// <summary>
        /// Get or set the mouse cursor position.
        /// </summary>
        public abstract Point CursorPosition { get; set; }

        /// <summary>
        /// Rotate the mouse wheel.
        /// </summary>
        public abstract void RotateMouseWheel(uint distance);

        /// <summary>
        /// Perform a key press or release.
        /// </summary>
        public abstract void PerformKey(int keycode, bool pushDown);
    }

    /// <summary>
    /// This input emulator uses SendInput and friends to emulate physical input.
    /// Mouse position and foreground window affect the physical mouse pointer and foreground window.
    /// </summary>
    public sealed class PhysicalInputEmulator : InputEmulator
    {
        static readonly uint[] MOUSE_FLAGS = { 0x00000002, 0x00000004, 0x00000008, 0x00000010, 0, 0, 0x00000020, 0x00000040 };
        static PhysicalInputEmulator instance;

        /// <summary>
        /// The singleton instance.
        /// </summary>
        public static PhysicalInputEmulator Instance
        {
            get
            {
                if (instance == null)
                    instance = new PhysicalInputEmulator();
                return instance;
            }
        }

        private PhysicalInputEmulator() { }

        ///
        public override SystemWindow ForegroundWindow
        {
            get { return SystemWindow.ForegroundWindow; }
            set { SystemWindow.ForegroundWindow = value; }
        }

        ///
        public override Point CursorPosition
        {
            get { return Cursor.Position; }
            set { Cursor.Position = value; }
        }

        ///
        public override void RotateMouseWheel(uint distance)
        {
            KeyboardKey.InjectMouseEvent(0x0800, 0, 0, distance, UIntPtr.Zero);
        }

        ///
        public override void PerformKey(int keycode, bool pushDown)
        {
            if (keycode == (int)Keys.LButton || keycode == (int)Keys.RButton || keycode == (int)Keys.MButton)
            {
                if (pushDown) KeyboardKey.InjectMouseEvent(MOUSE_FLAGS[keycode * 2 - 2], 0, 0, 0, UIntPtr.Zero);
                else KeyboardKey.InjectMouseEvent(MOUSE_FLAGS[keycode * 2 - 1], 0, 0, 0, UIntPtr.Zero);
            }
            else
            {
                KeyboardKey k = new KeyboardKey((Keys)keycode);
                if (pushDown) k.Press();
                else k.Release();
            }
        }
    }

    /// <summary>
    /// This input emulator uses window messages and holds an internal state to store
    /// mouse position, key states and foreground window.
    /// </summary>
    public class VirtualInputEmulator : InputEmulator
    {
        private SystemWindow foregroundWindow;
        private Point? cursorPosition;
        private bool[] pressedKeys = new bool[(int)Keys.KeyCode + 1];

        ///
        public override SystemWindow ForegroundWindow
        {
            get
            {
                if (foregroundWindow != null)
                {
                    string classname = "";
                    try
                    {
                        classname = foregroundWindow.ClassName;
                    }
                    catch (Win32Exception) { }
                    if (classname != "")
                    {
                        return foregroundWindow;
                    }
                    foregroundWindow = null;
                }
                return SystemWindow.ForegroundWindow;
            }
            set
            {
                foregroundWindow = value;
            }
        }

        ///
        public override Point CursorPosition
        {
            get
            {
                if (cursorPosition.HasValue)
                    return cursorPosition.Value;
                return Cursor.Position;
            }
            set
            {
                cursorPosition = value;
                PostMouseMessage(WM_MOUSEMOVE, 0);
            }
        }

        private SystemWindow FindWindowBelowPoint(Point mousePosition)
        {
            SystemWindow fg = ForegroundWindow;
            if (fg.Rectangle.ToRectangle().Contains(mousePosition))
                return fg;
            return SystemWindow.FromPointEx(mousePosition.X, mousePosition.Y, true, false);
        }

        private GUITHREADINFO GetCurrentWindowGUIThreadInfo()
        {
            GUITHREADINFO gti = new GUITHREADINFO();
            gti.cbSize = (uint)Marshal.SizeOf(gti);
            if (!GetGUIThreadInfo((uint)ForegroundWindow.Thread.Id, ref gti))
                throw new Win32Exception();
            return gti;
        }

        private bool IsExtended(int keycode)
        {
            switch ((Keys)keycode)
            {
                case Keys.Insert:
                case Keys.Delete:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Home:
                case Keys.End:
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                case Keys.RMenu:
                case Keys.RControlKey:
                    return true;
                default:
                    return false;
            }
        }

        private void PostMouseMessage(uint Msg, int wParamHigh)
        {
            GUITHREADINFO gti = GetCurrentWindowGUIThreadInfo();
            SystemWindow target = FindWindowBelowPoint(CursorPosition);
            if (gti.hwndCapture != IntPtr.Zero)
                target = new SystemWindow(gti.hwndCapture);
            int relX = CursorPosition.X - target.Rectangle.Left;
            int relY = CursorPosition.Y - target.Rectangle.Top;
            IntPtr wParam = new IntPtr(wParamHigh * 0x10000
                | (pressedKeys[(int)Keys.LButton] ? MK_LBUTTON : 0)
                | (pressedKeys[(int)Keys.MButton] ? MK_MBUTTON : 0)
                | (pressedKeys[(int)Keys.RButton] ? MK_RBUTTON : 0)
                | (pressedKeys[(int)Keys.XButton1] ? MK_XBUTTON1 : 0)
                | (pressedKeys[(int)Keys.XButton2] ? MK_XBUTTON2 : 0)
                | (pressedKeys[(int)Keys.ControlKey] ? MK_CONTROL : 0)
                | (pressedKeys[(int)Keys.ShiftKey] ? MK_SHIFT : 0));
            IntPtr lParam = new IntPtr(relX + relY * 0x10000);
            PostMessage(new HandleRef(target, target.HWnd), Msg, wParam, lParam);
        }

        ///
        public override void RotateMouseWheel(uint distance)
        {
            PostMouseMessage(WM_MOUSEWHEEL, (int)distance);
        }


        ///
        public override void PerformKey(int keycode, bool pushDown)
        {
            if (keycode == (int)Keys.LButton || keycode == (int)Keys.RButton || keycode == (int)Keys.MButton)
            {
                if (pushDown)
                    PostMouseMessage(MOUSE_MESSAGES[keycode * 3 - 3], 0);
                else
                    PostMouseMessage(MOUSE_MESSAGES[keycode * 3 - 2], 0);
                // TODO handle DoubleClick
            }
            else
            {
                GUITHREADINFO gti = GetCurrentWindowGUIThreadInfo();
                SystemWindow focus = new SystemWindow(gti.hwndFocus);
                if (!focus.IsDescendantOf(ForegroundWindow))
                    focus = ForegroundWindow;
                bool pressed = pressedKeys[keycode];
                IntPtr lParam = new IntPtr(1  // repeat count
                    | (0x10000 * (byte)MapVirtualKey(keycode, 0))  // scan code
                    | (IsExtended(keycode) ? 0x100000 : 0)  // extended key
                    | (pressed ? (1 << 30) : 0)  // previous key state
                    | (!pushDown ? (1 << 31) : 0)); // transition state

                if (pushDown)
                    PostMessage(new HandleRef(focus, focus.HWnd), WM_KEYDOWN, new IntPtr(keycode), lParam);
                else
                    PostMessage(new HandleRef(focus, focus.HWnd), WM_KEYUP, new IntPtr(keycode), lParam);
                pressedKeys[keycode] = pushDown;
            }
        }

        #region PInvoke Declarations

        private const int WM_MOUSEMOVE = 0x0200, WM_MOUSEWHEEL = 0x020A,
            WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_LBUTTONDBLCLK = 0x0203,
            WM_RBUTTONDOWN = 0x0204, WM_RBUTTONUP = 0x0205, WM_RBUTTONDBLCLK = 0x0206,
            WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208, WM_MBUTTONDBLCLK = 0x0209,
            WM_KEYDOWN = 0x0100, WM_KEYUP = 0x0101;

        private const int MK_LBUTTON = 0x0001, MK_RBUTTON = 0x0002, MK_MBUTTON = 0x0010,
            MK_SHIFT = 0x0004, MK_CONTROL = 0x0008,
            MK_XBUTTON1 = 0x0020, MK_XBUTTON2 = 0x0040;

        private static readonly uint[] MOUSE_MESSAGES = { 
                WM_LBUTTONDOWN, WM_LBUTTONUP, WM_LBUTTONDBLCLK,
                WM_RBUTTONDOWN, WM_RBUTTONUP, WM_RBUTTONDBLCLK,
                0, 0, 0, 
                WM_MBUTTONDOWN, WM_MBUTTONUP, WM_MBUTTONDBLCLK
            };

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool PostMessage(HandleRef hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        private static extern int MapVirtualKey(int uCode, int uMapType);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [Serializable, StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public uint cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }
        #endregion
    }
}
