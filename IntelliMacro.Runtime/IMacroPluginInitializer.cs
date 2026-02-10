using System;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// This interface is implemented by all plugin Init classes. These classes
    /// must be in a namespace that ends with "Commands" and is the same as the
    /// DLL that contains the plugin.
    /// </summary>
    public interface IMacroPluginInitializer
    {
        /// <summary>
        /// Called by the command registry to initialize the plugin.
        /// </summary>
        void InitPlugin(ICommandRegistry registry);
    }

    /// <summary>
    /// The command registry is responsible for keeping a list of supported
    /// commands and recording listeners.
    /// </summary>
    public interface ICommandRegistry
    {
        /// <summary>
        /// Register all commands defined as constants in a type.
        /// </summary>
        /// <param name="commandsType">The type that defines the constants</param>
        void RegisterAllCommands(Type commandsType);

        /// <summary>
        /// Register a recording listener.
        /// </summary>
        /// <param name="listener">The recording listener</param>
        void RegisterRecordingListener(IRecordingListener listener);
    }
}
