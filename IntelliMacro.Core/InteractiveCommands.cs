using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    static class InteractiveCommands
    {
        public static ICommand Stop = new StopCommand();
    }

    class StopCommand : AbstractCommand
    {
        internal StopCommand() : base("Stop", false, "&Stop execution", "C&ontrol flow") { }

        public override string Description
        {
            get
            {
                return "Stop macro execution and enter the debugger (if enabled).\n\n" +
                    "Use the 'Exit 0' command if you do not want to enter the debugger.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[0]; }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            throw new StopMacroException();
        }
    }
}