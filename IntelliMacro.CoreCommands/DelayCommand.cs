using System.Collections.Generic;
using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    class DelayCommand : AbstractCommand
    {
        internal DelayCommand() : base("Delay", false, "&Delay", "C&ontrol flow", false) { }

        public override string Description
        {
            get
            {
                return "Wait for a speicific number of milliseconds.\n" +
                    "Delays can be multiplied by the DelayMult command; this can be ignored by setting the second argument to 1.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Delay"),
                    new ParameterDescription(true, "Ignore delay multiplier")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            int delay = (int)parameters[0].Number;
            if (parameters[1] == null || parameters[1].Number == 0)
            {
                try
                {
                    delay *= context.DelayMultiplier;
                    delay /= context.DelayDivisor;
                }
                catch (KeyNotFoundException) { }
            }
            if (delay < 1) delay = 1;
            SetDelay((int)delay);
            return null;
        }
    }

    class DelayMultCommand : AbstractCommand
    {
        internal DelayMultCommand() : base("DelayMult", false, "Set Delay &Multiplier", "C&ontrol flow") { }

        public override string Description
        {
            get
            {
                return "Set a multiplier for delays.\n" +
                    "Use the multiplier to slow down delays (or disable them), or the divisor to speed them up.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "Multiplier"),
                    new ParameterDescription(true, "Divisor")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            context.DelayMultiplier = (int)parameters[0].Number;
            context.DelayDivisor = parameters[1] == null ? 1 : (int)parameters[1].Number;
            return null;
        }
    }
}
