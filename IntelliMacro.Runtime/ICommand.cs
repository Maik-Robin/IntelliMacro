using System;
using System.ComponentModel;
using System.Windows.Forms;
using ManagedWinapi;
using ManagedWinapi.Windows;

namespace IntelliMacro.Runtime
{
    /// <summary>
    /// Represents a command that can be run from an IntelliMacro macro.
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The name of the command. Commands are called by their name.
        /// Command names are case insensitive, but the beautifier will
        /// change all names to the case used here. By convention, command 
        /// names should be in PascalCase.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A longer name used to display in command lists or the Insert
        /// menu of IntelliMacro. An Ampersand sign (&amp;) can be used to
        /// specify a mnemonic.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// A category name used in the Inert menu of IntelliMacro. An
        /// ampersand sign (&amp;) can be used to specify a mnemonic.
        /// </summary>
        string DisplayCategory { get; }

        /// <summary>
        /// Whether this command returns a value. Commands that do not return
        /// a value have to return <c>null</c>.
        /// </summary>
        bool ReturnsValue { get; }

        /// <summary>
        /// How long the macro should wait after running this command.
        /// This value is only honored if the command does not return a value.
        /// If the value is negative, the same command will be called again after
        /// the delay.
        /// </summary>
        int Delay { get; }

        /// <summary>
        /// This value is only honored if the returned delay was EVENT_DELAY.
        /// In this case, a macro player implementation has to wait for this object
        /// before calling the command again (regardless of the delay given).
        /// </summary>
        IMacroEvent WaitEvent { get; }

        /// <summary>
        /// Descriptions of the parameters of this command.
        /// </summary>
        ParameterDescription[] ParameterDescriptions { get; }

        /// <summary>
        /// A descriptive text about this macro.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Invoke the command.
        /// </summary>
        /// <param name="context">The context to run this command in.</param>
        /// <param name="parameters">Parameters for this command.</param>
        /// <returns>A result object, if <see cref="ReturnsValue"/> is true, 
        /// or <c>null</c> otherwise.</returns>
        MacroObject Invoke(MacroContext context, MacroObject[] parameters);
    }

    /// <summary>
    /// An abstract base class for commands.
    /// </summary>
    public abstract class AbstractCommand : ICommand
    {
        readonly string name, displayName, displayCategory;
        bool checkWindow, returnsValue;
        int delay = 0;
        IMacroEvent waitEvent = null;

        /// <summary>
        /// If <see cref="ICommand.Delay"/> returns this value, wait for the wait event instead.
        /// </summary>
        public const int EVENT_DELAY = int.MinValue;

        /// <summary>
        /// Use this <see cref="MessageBoxOptions" /> value to show a message box that appears
        /// in front of all other programs. Unlike <see cref="MessageBoxOptions.ServiceNotification" />,
        /// this option will not appear in the event log and not show on the logon desktop.
        /// </summary>
        public const MessageBoxOptions MB_SYSTEMMODAL = ((MessageBoxOptions)4096);

        /// <summary>
        /// Create a instance of this command.
        /// </summary>
        /// <param name="name">The name of this command.</param>
        /// <param name="returnsValue">Whether the command returns a value.</param>
        /// <param name="displayName">The display name of this command.</param>
        /// <param name="displayCategory">The display category of this command.</param>
        protected AbstractCommand(string name, bool returnsValue, string displayName, string displayCategory) : this(name, returnsValue, displayName, displayCategory, true) { }

        /// <summary>
        /// Create a instance of this command.
        /// </summary>
        /// <param name="name">The name of this command.</param>
        /// <param name="returnsValue">Whether the command returns a value.</param>
        /// <param name="displayName">The display name of this command.</param>
        /// <param name="displayCategory">The display category of this command.</param>
        /// <param name="checkWindow">Whether this command checks that the foreground window did not change.</param>
        protected AbstractCommand(string name, bool returnsValue, string displayName, string displayCategory, bool checkWindow)
        {
            this.name = name;
            this.returnsValue = returnsValue;
            this.displayName = displayName;
            this.displayCategory = displayCategory;
            this.checkWindow = checkWindow;
            if (!returnsValue) delay = 1;
        }

        /// <see cref="ICommand.Name"/>
        public string Name { get { return name; } }

        /// <see cref="ICommand.DisplayName"/>
        public string DisplayName { get { return displayName; } }

        /// <see cref="ICommand.DisplayCategory"/>
        public string DisplayCategory { get { return displayCategory; } }

        /// <see cref="ICommand.ReturnsValue"/>
        public bool ReturnsValue { get { return returnsValue; } }

        /// <see cref="ICommand.Delay"/>
        public int Delay { get { return delay; } }

        /// <see cref="ICommand.WaitEvent"/>
        public IMacroEvent WaitEvent { get { return waitEvent; } }

        /// <summary>
        /// Call this method from the <see cref="Invoke"/> method to set the delay
        /// that should be waited after the command finishes.
        /// </summary>
        /// <see cref="Delay"/>
        /// <param name="delay">The delay that should be waited.</param>
        protected void SetDelay(int delay) { this.delay = delay; waitEvent = null; }

        /// <summary>
        /// Call this method from the <see cref="Invoke"/> method to set the event
        /// that should be waited for after the command finishes.
        /// </summary>
        /// <see cref="Delay"/>
        /// <param name="waitEvent">The event that should be waited for.</param>
        protected void SetWaitEvent(IMacroEvent waitEvent) { this.waitEvent = waitEvent; delay = AbstractCommand.EVENT_DELAY; }


        /// <see cref="ICommand.ParameterDescriptions"/>
        public abstract ParameterDescription[] ParameterDescriptions { get; }

        /// <see cref="ICommand.Description"/>
        public abstract string Description { get; }

        /// <summary>
        /// Called by the <see cref="Invoke"/> method to perform the actual action
        /// (Template-Method Pattern).
        /// </summary>
        /// <param name="context">The context to run this command in.</param>
        /// <param name="parameters">Parameters for this command.</param>
        /// <returns>A result object, if <see cref="ReturnsValue"/> is true, 
        /// or <c>null</c> otherwise.</returns>
        protected abstract MacroObject InvokeAction(MacroContext context, MacroObject[] parameters);

        /// <summary>
        /// Invoke the command. This method checks that the number of parameters 
        /// is correct and the context is still valid. Then it calls 
        /// <see cref="InvokeAction"/>.
        /// </summary>
        public MacroObject Invoke(MacroContext context, MacroObject[] parameters)
        {
            ParameterDescription[] descs = ParameterDescriptions;
            if (parameters.Length != descs.Length)
                throw new MacroErrorException("Invalid parameter count");
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i] == null && !descs[i].Optional)
                    throw new MacroErrorException("Mandatory parameter omitted!");
            }
            if (checkWindow && context.Window != null)
            {
                if (context.InputEmulator.ForegroundWindow.HWnd != context.Window.Window.HWnd)
                {
                    string classname = "";
                    try
                    {
                        classname = context.Window.Window.ClassName;
                    }
                    catch (Win32Exception) { }
                    if (classname == "")
                    {
                        MessageBox.Show("The current window has been closed! Macro stopped.", "IntelliMacro.NET", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MB_SYSTEMMODAL);
                        throw new StopMacroException();
                    }
                    if (MessageBox.Show("Window focus has changed! Stop macro?", "IntelliMacro.NET", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MB_SYSTEMMODAL) == DialogResult.Yes)
                        throw new StopMacroException();
                    context.InputEmulator.ForegroundWindow = context.Window.Window;
                }
            }
            if (context.MousePosition.HasValue)
            {
                if (!context.InputEmulator.CursorPosition.Equals(context.MousePosition.Value))
                {
                    if (MessageBox.Show("Mouse has been moved! Stop macro?", "IntelliMacro.NET", MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MB_SYSTEMMODAL) == DialogResult.Yes)
                        throw new StopMacroException();
                    context.InputEmulator.CursorPosition = context.MousePosition.Value;
                }
            }
            return InvokeAction(context, parameters);
        }
    }

    /// <summary>
    /// A abstract base class for commands that interacts with the user.
    /// This class will take care that input blocks are disabled while the command
    /// is executed.
    /// </summary>
    public abstract class InteractiveCommand : AbstractCommand
    {
        Form dummyWindow;

        /// <see cref="AbstractCommand(String,Boolean,String,String)"/>
        protected InteractiveCommand(string name, bool returnsValue, string displayName, string displayCategory) : base(name, returnsValue, displayName, displayCategory) { }

        /// <see cref="AbstractCommand(String,Boolean,String,String,Boolean)"/>
        protected InteractiveCommand(string name, bool returnsValue, string displayName, string displayCategory, bool checkWindow) : base(name, returnsValue, displayName, displayCategory, checkWindow) { }

        /// <summary>
        /// Invoke the command. This method calls <see cref="InvokeWithInput"/>
        /// with all input blockers disabled.
        /// </summary>
        protected sealed override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            context.MousePosition = null;
            context.Window = null;
            MacroObject result;
            if (context.InputBlocker == null)
            {
                result = InvokeWithInput(context, parameters);
            }
            else
            {
                context.InputBlocker.Dispose();
                context.InputBlocker = null;
                result = InvokeWithInput(context, parameters);
                context.InputBlocker = new InputBlocker();
            }
            if (dummyWindow != null)
            {
                dummyWindow.Dispose();
                dummyWindow = null;
            }
            return result;
        }

        /// <summary>
        /// Called by the <see cref="InvokeAction"/> method to perform the actual action
        /// (Template-Method Pattern).
        /// </summary>
        /// <param name="context">The context to run this command in.</param>
        /// <param name="parameters">Parameters for this command.</param>
        /// <returns>A result object, if <see cref="ICommand.ReturnsValue"/> is true, 
        /// or <c>null</c> otherwise.</returns>
        protected abstract MacroObject InvokeWithInput(MacroContext context, MacroObject[] parameters);

        /// <summary>
        /// Create a fully transparent dummy form which is visible in the center of the primary screen.
        /// The form will be disposed automatically when the command ends. Use this form as a parent
        /// for dialogs, which might appear below all other windows otherwise.
        /// </summary>
        protected Form DummyWindow
        {
            get
            {
                if (dummyWindow != null && !dummyWindow.Visible)
                {
                    dummyWindow.Dispose();
                    dummyWindow = null;
                }
                if (dummyWindow == null)
                {
                    dummyWindow = new Form()
                    {
                        Text = "IntelliMacro.NET",
                        FormBorderStyle = FormBorderStyle.None,
                        Width = 10,
                        Height = 10,
                        Opacity = 0,
                        StartPosition = FormStartPosition.CenterScreen
                    };
                    dummyWindow.Show();
                    SystemWindow.ForegroundWindow = new SystemWindow(dummyWindow);
                }
                return dummyWindow;
            }
        }
    }
}
