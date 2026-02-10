using System;
using System.Collections.Generic;
using System.Threading;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// This class is used by the generated C# source to control
    /// macro execution (like delays). It can also be used in other
    /// cases where macro commands should be used independent from IntelliMacro.NET.
    /// </summary>
    public class MacroCommandRunner
    {
        private readonly MacroContext context = new MacroContext();

        /// <summary>
        /// Use this macro command runner to invoke a command with parameters.
        /// </summary>
        /// <param name="command">The command to invoke</param>
        /// <param name="parameters">The parameters to pass</param>
        /// <returns></returns>
        public MacroObject Invoke(ICommand command, params MacroObject[] parameters)
        {
            while (true)
            {
                MacroObject result = command.Invoke(context, parameters);
                if (command.ReturnsValue) return result;
                int delay = command.Delay;
                if (delay == AbstractCommand.EVENT_DELAY)
                {
                    Utilities.WaitForEvent(command.WaitEvent);
                }
                else if (delay != 0)
                {
                    System.Windows.Forms.Application.DoEvents();
                    Thread.Sleep(Math.Abs(delay));
                    System.Windows.Forms.Application.DoEvents();
                }
                if (delay >= 0) return null;
            }
        }

        /// <summary>
        /// This method is called by generated C# code 
        /// and should not be called by user code.
        /// </summary>
        public long NextRandom(long upperBound)
        {
            return context.Random.Next((int)upperBound);
        }

        /// <summary>
        /// This method is called by generated C# code
        /// and should not be called by user code.
        /// </summary>
        public MacroList BuildList(params MacroObjectRange[] ranges)
        {
            List<MacroObject> list = new List<MacroObject>();
            foreach (MacroObjectRange r in ranges)
            {
                r.AppendTo(list);
            }
            return new MacroList(list);
        }
    }

    /// <summary>
    /// This class is used by the generated C# source 
    /// and should not be used by user code.
    /// </summary>
    public class MacroObjectRange
    {
        private readonly MacroObject first;
        private readonly MacroObject last;

        ///
        public MacroObjectRange(MacroObject first, MacroObject last)
        {
            if (first == null || last == null) throw new ArgumentNullException();
            this.first = first;
            this.last = last;
        }

        private MacroObjectRange(MacroObject obj)
        {
            this.first = obj;
            this.last = null;
        }

        ///
        public static implicit operator MacroObjectRange(MacroObject obj)
        {
            if (obj == null) throw new ArgumentNullException();
            return new MacroObjectRange(obj);
        }

        ///
        public void AppendTo(IList<MacroObject> target)
        {
            if (last == null)
            {
                target.Add(first);
                return;
            }
            long start = first.Number;
            long end = last.Number;
            long factor = start > end ? -1 : 1;
            for (long i = start * factor; i <= end * factor; i++)
            {
                target.Add(i * factor);
            }
        }
    }

    /// <summary>
    /// This class is used by the generated C# source 
    /// and should not be used by user code.
    /// </summary>
    public class ForLoopHandler
    {
        private bool downwards;
        private long fromValue;

        ///
        public ForLoopHandler(MacroObject from)
        {
            this.fromValue = from.Number;
            Value = fromValue;
        }

        ///
        public MacroObject Value { get; private set; }

        ///
        public bool HasNotExceeded(MacroObject to)
        {
            long toValue = to.Number;
            long valueValue = Value.Number;
            long fromValue = this.fromValue;
            downwards = fromValue > toValue;
            if (downwards)
            {
                fromValue = toValue;
                toValue = this.fromValue;
            }
            return valueValue >= fromValue && valueValue <= toValue;
        }

        ///
        public void Increment()
        {
            long current = Value.Number;
            current = current + (downwards ? -1 : 1);
            Value = current;
        }
    }
}
