using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using IntelliMacro.Runtime;

namespace IntelliMacro.Core
{
    class CommandRegistry : ICommandRegistry
    {
        internal static readonly CommandRegistry Instance = new CommandRegistry();

        Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
        Dictionary<string, Type> declaringTypes = new Dictionary<string, Type>();
        List<IRecordingListener> listeners = new List<IRecordingListener>();
        int pluginCount = 0;

        public void RegisterCommand(Type commandsType, ICommand command)
        {
            if (!Regex.IsMatch(command.Name, "^[A-Za-z0-9_]+$")) throw new Exception("Invalid characters in command name: " + command.Name);
            commands.Add(command.Name.ToUpperInvariant(), command);
            declaringTypes.Add(command.Name.ToUpperInvariant(), commandsType);
        }

        public void RegisterAllCommands(Type commandsType)
        {
            foreach (FieldInfo fi in commandsType.GetFields())
            {
                ICommand command = (ICommand)fi.GetValue(null);
                if (fi.Name != command.Name) throw new Exception("Different command names: " + fi.Name + " != " + command.Name);
                RegisterCommand(commandsType, command);
            }
        }

        public void RegisterRecordingListener(IRecordingListener listener)
        {
            listeners.Add(listener);
            listeners.Sort(delegate(IRecordingListener l1, IRecordingListener l2)
                {
                    return l1.Priority - l2.Priority;
                });
        }

        internal IList<IRecordingListener> RecordingListeners { get { return listeners; } }

        internal ICommand GetCommand(string name)
        {
            name = name.ToUpperInvariant();
            if (!commands.ContainsKey(name)) return null;
            return commands[name];
        }

        internal IEnumerable<ICommand> Commands
        {
            get { return commands.Values; }
        }

        internal int CommandCount
        {
            get { return commands.Count; }
        }

        internal int PluginCount
        {
            get { return pluginCount; }
        }

        internal void ScanPlugins()
        {
            RegisterAllCommands(typeof(InteractiveCommands));
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(Application.ExecutablePath), "*Commands.dll"))
            {
                Assembly a = Assembly.LoadFile(file);
                string initType = Path.GetFileName(file);
                if (!initType.EndsWith(".dll")) throw new Exception();
                initType = initType.Substring(0, initType.Length - 3) + "Init";
                Type t = a.GetType(initType, false);
                if (t != null)
                {
                    IMacroPluginInitializer mpi = t.GetConstructor(new Type[0]).Invoke(new object[0]) as IMacroPluginInitializer;
                    if (mpi != null)
                    {
                        mpi.InitPlugin(this);
                        pluginCount++;
                    }
                }
            }
        }

        internal Type GetDeclaringType(string commandName)
        {
            return declaringTypes[commandName.ToUpperInvariant()];
        }
    }
}
