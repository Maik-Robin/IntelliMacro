using IntelliMacro.Runtime;
using ManagedWinapi.Windows;

namespace IntelliMacro.CoreCommands
{
    class BrowseCommand : AbstractCommand
    {
        public BrowseCommand() : base("Browse", false, "&Browse to URL", "Brows&er") { }

        public override string Description
        {
            get
            {
                return "Browse to URL\n\n" +
                    "This will start the IntelliMacro.NET Mini Web Browser (if not already started) and browse to the given URL.\n" +
                    "If WaitForLoading is set and not zero, wait until the web page has completely loaded" +
                    "Other BrowseXXX commands can be used to control this web browser window.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(true, "URL"),
                    new ParameterDescription(true, "WaitForLoading"),
                };
            }
        }

        private bool nextTimeReturn = false;

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            if (nextTimeReturn)
            {
                nextTimeReturn = false;
                SetDelay(1);
                return null;
            }
            BrowseWindow.Instance.Show();
            SystemWindow.ForegroundWindow = new SystemWindow(BrowseWindow.Instance);
            if (parameters[0] != null)
            {
                BrowseWindow.Instance.BrowseToURL(parameters[0].String);
            }
            if (parameters[1] != null && parameters[1].Number != 0 && !BrowseWindow.Instance.HasOccurred)
            {
                SetWaitEvent(BrowseWindow.Instance);
                nextTimeReturn = true;
            }
            else
            {
                SetDelay(1);
            }
            return null;
        }
    }

    class BrowseFormElementCommand : AbstractCommand
    {
        public BrowseFormElementCommand() : base("BrowseFormElement", false, "Activate &Form element", "Brows&er") { }

        public override string Description
        {
            get
            {
                return "Activate a browser form element.\n\n" +
                    "Buttons will be clicked, checkboxes/radio buttons toggled, input fields focused or filled.\n" +
                    "The optional value parameter is used for filling form fields and for distinguishing elements with same name, like radio buttons.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "formName"),
                    new ParameterDescription(false, "name"),
                    new ParameterDescription(true, "value")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            BrowseWindow.Instance.ExecuteJavaScript("IntelliMacroBrowserScripting_FormElementAction", parameters[0].String, parameters[1].String, parameters[2] == null ? null : parameters[2].String);
            SetDelay(1);
            return null;
        }
    }

    class BrowseNavigateCommand : AbstractCommand
    {
        public BrowseNavigateCommand() : base("BrowseNavigate", false, "&Navigate", "Brows&er") { }

        public override string Description
        {
            get
            {
                return "Navigate in the browser.}n\n" +
                    @"Supported directions are""Back"", ""Forward"", ""Stop"" and ""Reload"".";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "direction")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            BrowseWindow.Instance.Navigate(parameters[0].String);
            SetDelay(1);
            return null;
        }
    }

    class BrowseEvalCommand : AbstractCommand
    {
        public BrowseEvalCommand() : base("BrowseEval", true, "&Evaluate Javascriptexpression", "Brows&er") { }

        public override string Description
        {
            get
            {
                return "Evaluate a javascript expression\n\n" +
                    "The result is returned as a string.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "expression")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            return BrowseWindow.Instance.ExecuteJavaScript("eval", parameters[0].String);
        }
    }

    class BrowseRunCommand : AbstractCommand
    {
        public BrowseRunCommand() : base("BrowseRun", false, "&Run Javascript command", "Brows&er") { }

        public override string Description
        {
            get
            {
                return "Run a javascript command\n\n" +
                    "The result is discarded.";
            }
        }

        public override ParameterDescription[] ParameterDescriptions
        {
            get
            {
                return new ParameterDescription[] {
                    new ParameterDescription(false, "command")
                };
            }
        }

        protected override MacroObject InvokeAction(MacroContext context, MacroObject[] parameters)
        {
            BrowseWindow.Instance.ExecuteJavaScript("eval", parameters[0].String);
            SetDelay(1);
            return null;
        }
    }
}
