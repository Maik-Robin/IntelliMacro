using System;
using System.Windows.Forms;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// A class that contains generic utility functions.
    /// </summary>
    public static class Utilities
    {
        /// <summary>
        /// Invoke a delegate later from a timer.
        /// </summary>
        public static void InvokeLater(EventHandler delayedDelegate)
        {
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Tick += delegate(object sender, EventArgs e)
            {
                t.Enabled = false;
                delayedDelegate(sender, e);
            };
            t.Interval = 10;
            t.Enabled = true;
        }

        /// <summary>
        /// Wait for the occurrence of a macro event.
        /// </summary>
        /// <param name="macroEvent">The event to wait for</param>
        internal static void WaitForEvent(IMacroEvent macroEvent)
        {
            object myLock = new object();
            EventHandler myHandler = new EventHandler(delegate
            {
                lock (myLock)
                {
                    System.Threading.Monitor.PulseAll(myLock);
                }
            });
            macroEvent.Occurred += myHandler;
            lock (myLock)
            {
                while (!macroEvent.HasOccurred)
                {
                    System.Threading.Monitor.Wait(myLock);
                }
            }
            macroEvent.Occurred -= myHandler;
        }
    }
}
