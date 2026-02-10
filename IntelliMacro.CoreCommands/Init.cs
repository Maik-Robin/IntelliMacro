using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    internal class Init : IMacroPluginInitializer
    {
        public void InitPlugin(ICommandRegistry registry)
        {
            registry.RegisterAllCommands(typeof(CoreCommands));
            registry.RegisterRecordingListener(new CoreRecordingListener());
            registry.RegisterRecordingListener(new WindowChangeRecordingListener());
            registry.RegisterRecordingListener(BrowseRecordingListener.Instance);
        }
    }
}
