using System;
using IntelliMacro.Runtime;
using ManagedWinapi;

namespace IntelliMacro.CoreCommands
{
    class GetDateCommand : AbstractCommand
    {
        internal GetDateCommand() : base("GetDate", true, "&Get date", "&Date/time functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "base"),
                    new ParameterDescription(true, "addYears"),
                    new ParameterDescription(true, "addMonths"),
                    new ParameterDescription(true, "addDays"),
                    new ParameterDescription(true, "addSeconds"),
                    new ParameterDescription(true, "weekdayConstraint")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Get the current system date/time or another date/time.\n" +
                    "This command takes a base date, optionally adds some years/months/days, optionall shifts the result to be on a specific weekday, and returns it.\n" +
                    "If the base date is absent, current date/time is used. If it is a number, it is parsed as a tick count. If it is a list in the form [year month day hour minute second millisecond] or a prefix of that it is parsed like that.\n" +
                    "A year/month/date of 0 means today, month=-1 means easter in this year, negative days count from the end of the month.\n" +
                    "A hour/minute/second/millisecond of -1 means current, 0 means what you expect.\n" +
                    "If an addYears/months/days/seconds valus is an array, each value is added individually and it is checked whether the result 'jumps over' today. In case of a jump, the first result after the jump is taken, otherwise the first result." +
                    "If weekdayConstraint is positive, look for the next day that matches the weekday (Sunday=1, Monday=2 etc.). If negative, look for the previous day.\n" +
                    "If weekdayConstraints is an array, each value is tried subsequently, and the nearest day is taken, e. g. [2,-2] will select the monday before or after this day, whichever is nearer.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            DateTime date, now = DateTime.Now;
            if (parameters[0] == null)
            {
                date = now;
            }
            else if (parameters[0] is MacroList)
            {
                int[] components = new int[7];
                for (int i = 0; i < parameters[0].Length && i < 7; i++)
                {
                    components[i] = (int)parameters[0][i + 1].Number;
                }
                if (components[0] == 0) components[0] = now.Year;
                if (components[1] == 0) components[1] = now.Month;
                if (components[1] == -1)
                {
                    int Y = components[0];
                    int a = Y % 19;
                    int b = Y / 100;
                    int c = Y % 100;
                    int d = b / 4;
                    int e = b % 4;
                    int f = (b + 8) / 25;
                    int g = (b - f + 1) / 3;
                    int h = (19 * a + b - d - g + 15) % 30;
                    int i = c / 4;
                    int k = c % 4;
                    int L = (32 + 2 * e + 2 * i - h - k) % 7;
                    int m = (a + 11 * h + 22 * L) / 451;
                    int month = (h + L - 7 * m + 114) / 31;
                    int day = ((h + L - 7 * m + 114) % 31) + 1;
                    components[1] = month;
                    components[2] = day;
                }
                if (components[2] == 0) components[2] = now.Day;
                if (components[2] < 0)
                {
                    components[2] = new DateTime(components[0], components[1], 1).AddMonths(1).AddDays(-1).Day;
                }
                if (components[3] == -1) components[3] = now.Hour;
                if (components[4] == -1) components[4] = now.Minute;
                if (components[5] == -1) components[5] = now.Second;
                if (components[6] == -1) components[6] = now.Millisecond;
                date = new DateTime(components[0], components[1], components[2],
                    components[3], components[4], components[5], components[6]);
            }
            else
            {
                date = new DateTime(parameters[0].Number);
            }
            if (parameters[1] != null)
                date = DoAdd(date, parameters[1], typeof(DateTime).GetMethod("AddYears"), now);
            if (parameters[2] != null)
                date = DoAdd(date, parameters[2], typeof(DateTime).GetMethod("AddMonths"), now);
            if (parameters[3] != null)
                date = DoAdd(date, parameters[3], typeof(DateTime).GetMethod("AddDays"), now);
            if (parameters[4] != null)
                date = DoAdd(date, parameters[4], typeof(DateTime).GetMethod("AddSeconds"), now);
            if (parameters[5] != null)
            {
                int[] wdc;
                if (parameters[5] is MacroList)
                {
                    wdc = new int[parameters[5].Length];
                    for (int i = 0; i < wdc.Length; i++)
                    {
                        wdc[i] = (int)parameters[5][i + 1].Number;
                    }
                }
                else
                {
                    wdc = new int[] { (int)parameters[5].Number };
                }
                DateTime bestResult = DateTime.MinValue;
                for (int i = 0; i < wdc.Length; i++)
                {
                    DateTime nextResult = date;
                    if (wdc[i] != 0)
                    {
                        while (nextResult.DayOfWeek != (DayOfWeek)(Math.Abs(wdc[i]) - 1))
                        {
                            nextResult = nextResult.AddDays(wdc[i] > 0 ? 1 : -1);
                        }
                    }
                    if (Math.Abs(nextResult.Ticks - date.Ticks) < Math.Abs(bestResult.Ticks - date.Ticks))
                        bestResult = nextResult;
                }
                date = bestResult;
            }
            return date.Ticks;
        }

        private DateTime DoAdd(DateTime date, MacroObject value, System.Reflection.MethodInfo methodInfo, DateTime now)
        {
            int[] options;
            if (value is MacroList)
            {
                options = new int[value.Length];
                for (int i = 0; i < options.Length; i++)
                {
                    options[i] = (int)value[i + 1].Number;
                }
            }
            else
            {
                options = new int[] { (int)value.Number };
            }
            DateTime result = date;
            int lastDirection = 2;
            for (int i = 0; i < options.Length; i++)
            {
                DateTime nextResult = (DateTime)methodInfo.Invoke(date, new object[] { options[i] });
                int direction = 0;
                if (nextResult > now) direction = 1;
                if (nextResult < now) direction = -1;
                if (direction != lastDirection) { result = nextResult; lastDirection = direction; }
            }
            return result;
        }
    }

    class DateInfoCommand : AbstractCommand
    {
        internal DateInfoCommand() : base("DateInfo", true, "Date &info", "&Date/time functions") { }
        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "date"),
                    new ParameterDescription(true, "format")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Returns the date formatted by format.\n" +
                    "If format is not given, an array of numbers is returned.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            DateTime date = new DateTime(parameters[0].Number);
            if (parameters[1] != null && !parameters[1].IsNumber)
            {
                return date.ToString(parameters[1].String);
            }
            int[] infos = new int[] {
                date.Year, date.Month, date.Day,
                date.Hour, date.Minute, date.Second, date.Millisecond,
                (int)date.DayOfWeek + 1, date.DayOfYear
            };
            MacroObject result = new MacroList(Array.ConvertAll(infos, x => (MacroObject)x));
            if (parameters[1] != null)
                result = result[parameters[1]];
            return result;
        }
    }

    class SetDateCommand : AbstractCommand
    {
        internal SetDateCommand() : base("SetDate", false, "&Set date", "&Date/time functions") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "newDate")
                };
            }
        }

        public override string Description
        {
            get { return "Sets the system date to the date given"; }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            DateTime dt;
            try
            {
                if (parameters[0].IsNumber)
                    dt = new DateTime(parameters[0].Number);
                else
                    dt = DateTime.Parse(parameters[0].String);
                PrivilegedActions.LocalTime = dt;
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            SetDelay(1);
            return null;
        }
    }
}
