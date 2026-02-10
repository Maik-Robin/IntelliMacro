using System;
using System.Collections.Generic;
using System.Diagnostics;
using IntelliMacro.Runtime;
using IntelliMacro.Runtime.Paths;

namespace IntelliMacro.CoreCommands
{
    class RunCommand : AbstractCommand
    {
        internal RunCommand() : base("Run", false, "&Run process", "&Processes") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "command"),
                    new ParameterDescription(true, "arguments"),
                    new ParameterDescription(true, "hidden"),
                    new ParameterDescription(true, "waitFor"),
                    new ParameterDescription(true, "shellExecute"),
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Run an external program.\n" +
                    "If hidden is 1, the program is hidden. If hidden is 2 or 3, the program is run minimized or maximized.\n" +
                    "If waitFor is 1, wait until program ends, if it is 2, wait until program is waiting for input.\n" +
                    "If shellExecute is 1, use ShellExecute to run the file (like Explorer does), if it is a string, use the string as ShellExecute verb (like 'Open' or 'Print').";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            ProcessStartInfo psi = new ProcessStartInfo(parameters[0].String, parameters[1] == null ? "" : parameters[1].String);
            if (parameters[4] == null)
            {
                psi.UseShellExecute = false;
            }
            else if (parameters[4].IsNumber)
            {
                psi.UseShellExecute = parameters[4].Number != 0;
            }
            else
            {
                psi.UseShellExecute = true;
                psi.Verb = parameters[4].String;
            }
            if (parameters[2] == null)
            {
                psi.WindowStyle = ProcessWindowStyle.Normal;
            }
            else if (parameters[2].IsNumber && parameters[2].Number >= 0 && parameters[2].Number <= 3)
            {
                psi.WindowStyle = (ProcessWindowStyle)parameters[2].Number;
            }
            else
            {
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }
            try
            {
                Process p = Process.Start(psi);
                if (parameters[3] != null && parameters[3].IsNumber)
                {
                    if (parameters[3].Number == 1)
                    {
                        p.WaitForExit();
                    }
                    else if (parameters[3].Number == 2)
                    {
                        p.WaitForInputIdle();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new MacroErrorException(ex.Message);
            }
            return null;
        }
    }

    class SetEnvCommand : AbstractCommand
    {
        public SetEnvCommand() : base("SetEnv", false, "&Set environment variable", "&Processes") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "name"),
                    new ParameterDescription(false, "value")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Set an environment variable.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            Environment.SetEnvironmentVariable(parameters[0].String, parameters[1].String);
            return null;
        }
    }

    class GetEnvCommand : AbstractCommand
    {
        public GetEnvCommand() : base("GetEnv", true, "&Get environment variable", "&Processes") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get { return new ParameterDescription[] { new ParameterDescription(false, "variable") }; }
        }

        public override string Description
        {
            get
            {
                return "Get the value of an environment variable.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            return Environment.GetEnvironmentVariable(parameters[0].String);
        }
    }

    class FindProcessesCommand : AbstractCommand
    {
        internal FindProcessesCommand() : base("FindProcesses", true, "&Find Processes", "&Processes") { }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "pattern")
                };
            }
        }

        public override string Description
        {
            get
            {
                return "Find Processes that match a pattern.";
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            string pattern = parameters[0].String;
            List<MacroObject> result = new List<MacroObject>();
            foreach (ProcessNode pn in PathParser.ParsePath(pattern, ProcessRoot.instance))
            {
                result.Add(pn.PID);
            }
            return new MacroList(result);
        }
    }

    class ProcessInfoCommand : AbstractPathNodeInfoCommand<ProcessNode>
    {
        internal ProcessInfoCommand() : base("ProcessInfo", "Process &Information", "&Processes") { }

        protected override string PathName
        {
            get
            {
                return "PID";
            }
        }

        public override string Description
        {
            get
            {
                return "Obtain a parameter of a process and return it.\n" +
                    base.Description;
            }
        }

        protected override ProcessNode GetPathNode(MacroObject path, MacroContext context)
        {
            if (path.IsNumber)
            {
                return new ProcessNode((int)path.Number);
            }
            else
            {
                return PathParser.ParsePath(ProcessRoot.instance, path.String, "Process");
            }
        }
    }

    class ProcessRoot : IPathRoot<ProcessNode>
    {
        public static ProcessRoot instance = new ProcessRoot();

        public IEnumerable<ProcessNode> Children
        {
            get
            {
                return Array.ConvertAll(Process.GetProcesses(), x => new ProcessNode(x));
            }
        }

        public IPathRoot<ProcessNode> Parent { get { return null; } }
    }

    class ProcessNode : IPathNode<ProcessNode>
    {
        Process proc;
        int pid;

        public ProcessNode(Process proc)
        {
            this.proc = proc;
            this.pid = proc.Id;
        }

        public ProcessNode(int pid)
        {
            this.pid = pid;
            try
            {
                this.proc = Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                this.proc = null;
            }
        }

        public int PID { get { return pid; } }
        public IEnumerable<ProcessNode> Children { get { return new ProcessNode[0]; } }
        public IPathRoot<ProcessNode> Parent { get { return null; } }

        public string NodeName
        {
            get { return proc == null ? "?" : proc.ProcessName; }
        }

        public IEnumerable<string> ParameterNames
        {
            get
            {
                return new string[] {
                    "pid", "priority", "exited", "file", "myself"
                };
            }
        }

        public string GetParameter(string name)
        {
            if (name.ToLowerInvariant() == "pid")
                return "" + pid;
            if (proc == null) return "";
            try
            {
                switch (name.ToLowerInvariant())
                {
                    case "priority": return "" + proc.BasePriority;
                    case "exited": return proc.HasExited ? "1" : "";
                    case "file": return proc.MainModule.FileName.ToUpperInvariant();
                    case "myself": return pid == Process.GetCurrentProcess().Id ? "1" : "";
                    default: return "";
                }
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }
}