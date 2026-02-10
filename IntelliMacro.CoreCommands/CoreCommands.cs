using IntelliMacro.Runtime;

namespace IntelliMacro.CoreCommands
{
    public static class CoreCommands
    {
        public static readonly ICommand Down = new KeyboardCommand("Down", true, false);
        public static readonly ICommand Up = new KeyboardCommand("Up", false, true);
        public static readonly ICommand DownUp = new KeyboardCommand("DownUp", true, true);
        public static readonly ICommand SendKeys = new SendKeysCommand(false);
        public static readonly ICommand SendText = new SendKeysCommand(true);
        public static readonly ICommand VirtualInput = new VirtualInputCommand();

        public static readonly ICommand Mouse = new MouseCommand("Mouse", false, false);
        public static readonly ICommand MouseRel = new MouseCommand("MouseRel", true, false);
        public static readonly ICommand MouseRelWindow = new MouseCommand("MouseRelWindow", true, true);
        public static readonly ICommand MouseRelCtrl = new ControlBasedMouseCommand();
        public static readonly ICommand Wheel = new WheelCommand();
        public static readonly ICommand GetMouse = new GetMouseCommand();
        public static readonly ICommand RegisterHotkey = new RegisterHotkeyCommand();
        public static readonly ICommand UnregisterHotkey = new UnregisterHotkeyCommand();

        public static readonly ICommand Window = new WindowCommand();
        public static readonly ICommand FindWindow = new FindWindowCommand();
        public static readonly ICommand FindWindows = new FindWindowsCommand();
        public static readonly ICommand WindowFromPoint = new WindowFromPointCommand();
        public static readonly ICommand GetPosition = new GetPositionCommand("Position");
        public static readonly ICommand GetRectangle = new GetPositionCommand("Rectangle");
        public static readonly ICommand SetPosition = new SetPositionCommand();
        public static readonly ICommand WindowInfo = new WindowInfoCommand();

        public static readonly ICommand Msg = new MessageCommand();
        public static readonly ICommand Confirm = new ConfirmCommand();
        public static readonly ICommand FileDialog = new FileDialogCommand();
        public static readonly ICommand Input = new InputCommand();
        public static readonly ICommand BlockInput = new BlockInputCommand();
        public static readonly ICommand KeyPressed = new KeyPressedCommand();
        public static readonly ICommand WaitForKey = new WaitForKeyCommand();
        public static readonly ICommand Select = new SelectCommand();

        public static readonly ICommand Console = new ConsoleCommand();
        public static readonly ICommand AddConsole = new AddConsoleCommand();
        public static readonly ICommand ReadConsole = new ReadConsoleCommand();

        public static readonly ICommand Delay = new DelayCommand();
        public static readonly ICommand DelayMult = new DelayMultCommand();

        public static readonly ICommand ClearClipboard = new ClearClipboardCommand();
        public static readonly ICommand GetClipboard = new GetClipboardCommand();
        public static readonly ICommand SetClipboard = new SetClipboardCommand();

        public static readonly ICommand Find = new FindCommand();
        public static readonly ICommand Sort = new SortCommand();

        public static readonly ICommand ScreenSize = new ScreenSizeCommand();
        public static readonly ICommand GetColor = new GetColorCommand();

        public static readonly ICommand FindAccObj = new FindAccObjCommand();
        public static readonly ICommand FindAccObjs = new FindAccObjsCommand();
        public static readonly ICommand AccObjFromPoint = new AccObjFromPointCommand();
        public static readonly ICommand AccObjInfo = new AccObjInfoCommand();
        public static readonly ICommand InvokeAccObj = new InvokeAccObjCommand();
        public static readonly ICommand Menu = new MenuCommand();

        public static readonly ICommand GetDate = new GetDateCommand();
        public static readonly ICommand DateInfo = new DateInfoCommand();
        public static readonly ICommand SetDate = new SetDateCommand();

        public static readonly ICommand Run = new RunCommand();
        public static readonly ICommand SetEnv = new SetEnvCommand();
        public static readonly ICommand GetEnv = new GetEnvCommand();
        public static readonly ICommand FindProcesses = new FindProcessesCommand();
        public static readonly ICommand ProcessInfo = new ProcessInfoCommand();

        public static readonly ICommand Trim = new TrimCommand();
        public static readonly ICommand LCase = new LCaseCommand();
        public static readonly ICommand UCase = new UCaseCommand();
        public static readonly ICommand Replace = new ReplaceCommand();
        public static readonly ICommand Join = new JoinCommand();
        public static readonly ICommand Split = new SplitCommand();
        public static readonly ICommand Format = new FormatCommand();
        public static readonly ICommand Serialize = new SerializeCommand();

        public static readonly ICommand FindFiles = new FindFilesCommand();
        public static readonly ICommand FileInfo = new FileInfoCommand();
        public static readonly ICommand ChangeDir = new ChangeDirCommand();
        public static readonly ICommand DeleteFile = new DeleteFileCommand();
        public static readonly ICommand CopyFile = new MoveFileCommand(true);
        public static readonly ICommand MoveFile = new MoveFileCommand(false);
        public static readonly ICommand SaveFile = new SaveFileCommand();
        public static readonly ICommand LoadFile = new LoadFileCommand();

        public static readonly ICommand Browse = new BrowseCommand();
        public static readonly ICommand BrowseFormElement = new BrowseFormElementCommand();
        public static readonly ICommand BrowseNavigate = new BrowseNavigateCommand();
        public static readonly ICommand BrowseEval = new BrowseEvalCommand();
        public static readonly ICommand BrowseRun = new BrowseRunCommand();
    }
}
