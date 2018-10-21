namespace CrypticButter.ButteryTaskbar
{
    using Microsoft.Win32;
    using System;
    using System.Deployment.Application;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows.Forms;

    public enum TaskbarPosition
    {
        Unknown = -1,
        Left,
        Top,
        Right,
        Bottom,
    }

    public enum MONITOR_APP_VISIBILITY
    {
        MAV_UNKNOWN,
        MAV_NO_APP_VISIBLE,
        MAV_APP_VISIBLE,
    }

    public enum HRESULT : long
    {
        S_OK = 0x00000000, // Operation successful
        E_NOTIMPL = 0x80004001, // Not implemented
        E_NOINTERFACE = 0x80004002, // No such interface supported
        E_POINTER = 0x80004003, // Pointer that is not valid
        E_ABORT = 0x80004004, // Operation aborted
        E_FAIL = 0x80004005, // Unspecified failure
        E_UNEXPECTED = 0x8000FFFF, // Unexpected failure
        E_ACCESSDENIED = 0x80070005, // General access denied error
        E_HANDLE = 0x80070006, // Handle that is not valid
        E_OUTOFMEMORY = 0x8007000E, // Failed to allocate necessary memory
        E_INVALIDARG = 0x80070057, // One or more arguments are not valid
    }

    #region App visibility

    [ComImport, Guid("2246EA2D-CAEA-4444-A3C4-6DE827E44313"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibility
    {
        HRESULT GetAppVisibilityOnMonitor(
            [In] IntPtr hMonitor,
            [Out] out MONITOR_APP_VISIBILITY pMode);

        HRESULT IsLauncherVisible(
            [Out] out bool pfVisible);

        HRESULT Advise(
            [In] IAppVisibilityEvents pCallback,
            [Out] out int pdwCookie);

        HRESULT Unadvise(
            [In] int dwCookie);
    }

    [ComImport, Guid("6584CE6B-7D82-49C2-89C9-C6BC02BA8C38"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAppVisibilityEvents
    {
        HRESULT AppVisibilityOnMonitorChanged(
            [In] IntPtr hMonitor,
            [In] MONITOR_APP_VISIBILITY previousMode,
            [In] MONITOR_APP_VISIBILITY currentMode);

        HRESULT LauncherVisibilityChange(
            [In] bool currentVisibleState);
    }
    #endregion

    [StructLayout(LayoutKind.Sequential)]
    public struct APPBARDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uCallbackMessage;
        public uint uEdge;
        public RECT rc;
        public int lParam;

        /// <summary>
        /// Creates a new instance of APPBARDATA and sets cbSize
        /// </summary>
        /// <returns></returns>
        public static APPBARDATA NewInstance()
        {
            var appBarData = new APPBARDATA
            {
                cbSize = GetSizeOfAPPBARDATAType(),
            };
            return appBarData;
        }

        /// <summary>
        /// Used for setting cbSize
        /// </summary>
        /// <returns></returns>
        public static int GetSizeOfAPPBARDATAType() => Marshal.SizeOf(typeof(APPBARDATA));
    }

    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    internal static partial class Shell32
    {
        #region ShellAPI Constants

        public const int ABM_NEW = 0x00000000;
        public const int ABM_REMOVE = 0x00000001;
        public const int ABM_QUERYPOS = 0x00000002;
        public const int ABM_SETPOS = 0x00000003;
        public const int ABM_GETSTATE = 0x00000004;
        public const int ABM_GETTASKBARPOS = 0x00000005;
        public const int ABM_ACTIVATE = 0x00000006;
        public const int ABM_GETAUTOHIDEBAR = 0x00000007;
        public const int ABM_SETAUTOHIDEBAR = 0x00000008;

        public const int ABS_AUTOHIDE = 0x0000001;
        public const int ABS_ALWAYSONTOP = 0x0000002;

        public const int ABE_LEFT = 0;
        public const int ABE_TOP = 1;
        public const int ABE_RIGHT = 2;
        public const int ABE_BOTTOM = 3;
        #endregion

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(uint dwMessage, [In] ref APPBARDATA pData);
    }

    internal static partial class User32
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOW = 5;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }

    /// <summary>
    /// Program to hide the taskbar when the Start Menu is not open
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The milliseconds between each update of the taskbar's visibility
        /// </summary>
        private const int UpdateTaskbarVisibilityInterval = 20;

        /// <summary>
        /// The timeout after setting the taskbar's visibility, in milliseconds
        /// </summary>
        private const int TaskbarVisibilityChangedTimeout = 300;


        /// <summary>
        /// The URL for the program's help/support webpage
        /// </summary>
        private const string HelpLocation = "https://github.com/CrypticButter/ButteryTaskbar/wiki/The-Buttery-Taskbar-Wiki";

        /// <summary>
        /// The name of the file of the application icon
        /// </summary>
        private const string ApplicationIconFileName = "AppIconRound.ico";

        public static readonly string ApplicationName = "Buttery Taskbar";

        private static bool _disabled;
        public static bool Disabled
        {
            get => _disabled;
            set
            {
                _disabled = value;
                if (_disabled)
                {
                    _updateVisibilityTimer.Stop();
                }
                else
                {
                    _updateVisibilityTimer.Start();
                }
            }
        }

        /// <summary>
        /// Represents the taskbar
        /// </summary>
        private static Taskbar _taskbar = new Taskbar();

        /// <summary>
        /// Used to trigger the updating of the taskbar at regular intervals
        /// </summary>
        private static System.Timers.Timer _updateVisibilityTimer;

        /// <summary>
        /// Whether the application expects the taskbar to be visible
        /// </summary>
        private static bool _shouldTaskbarBeVisible = false;

        /// <summary>
        /// If the Start Menu was visible before re-checking
        /// </summary>
        private static bool? _wasStartMenuVisibleBefore = null;

        /// <summary>
        /// The tray icon for the application, with access to settings
        /// </summary>
        private static NotifyIcon _appSettingsNotifyIcon;

        /// <summary>
        /// The application icon for the system tray
        /// </summary>
        private static Icon _applicationIcon = new Icon(ApplicationIconFileName);

        /// <summary>
        /// The context menu of the application's tray icon
        /// </summary>
        private static ContextMenu _trayContextMenu = new ContextMenu();

        /// <summary>
        /// Tray menu item for whether app should start automatically
        /// </summary>
        private static MenuItem _autoStartupMenuItem;

        /// <summary>
        /// Tray menu item for whether app should attempt to keep the taskbar hidden if the Start Menu is closed
        /// </summary>
        private static MenuItem _forceTaskbarStateMenuItem;

        /// <summary>
        /// Tray menu item for whether app should attempt to keep the taskbar hidden if the Start Menu is closed
        /// </summary>
        private static MenuItem _keepTaskbarIfCursorNearMenuItem;

        /// <summary>
        /// Tray menu item for disabling the functionality of the programme
        /// </summary>
        private static MenuItem _disableAppMenuItem;

        private static RegistryKey _startupAppsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        private static string _applicationPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + $@"\Cryptic Butter\{Program.ApplicationName}.appref-ms";

        private static void Main(string[] args)
        {
            var appMutex = new Mutex(true, ApplicationName, out bool newMutexCreated);

            if (!newMutexCreated)
            {
                throw new Exception("Buttery Taskbar is already running!");
            }

            SetIcon();

            _updateVisibilityTimer = new System.Timers.Timer
            {
                Interval = UpdateTaskbarVisibilityInterval,
                AutoReset = false,
            };
            _updateVisibilityTimer.Elapsed += (s, e) => UpdateTaskbarVisibility();
            _updateVisibilityTimer.Start();

            SetAutoStartupState(Properties.Settings.Default.AutoStartup);

            string appVersionNumber = ApplicationDeployment.IsNetworkDeployed
                ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                : "Dev";

            _disableAppMenuItem = new MenuItem("Disable", DisableApppMenuItem_Click)
            {
                Checked = false,
            };
            _forceTaskbarStateMenuItem = new MenuItem("Keep taskbar hidden", ForceStateMenuItem_Click)
            {
                Checked = Properties.Settings.Default.ForceTaskbarState,
            };
            // TODO KTOCN
            /*_keepTaskbarIfCursorNearMenuItem = new MenuItem("Hide taskbar after cursor moves away", KeepTaskbarCursorNearMenuItem_Click)
            {
                Checked = Properties.Settings.Default.KeepTaskbarOpenIfCursorNear,
            };*/
            _autoStartupMenuItem = new MenuItem("Start with Windows", AutoStartupMenuItem_Click)
            {
                Checked = Properties.Settings.Default.AutoStartup,
            };

            var helpMenuItem = new MenuItem("Help", HelpMenuItem_Click);
            var quitMenuItem = new MenuItem("Quit", QuitMenuItem_Click);
            var appNameMenuItem = new MenuItem($"{Program.ApplicationName} {appVersionNumber}")
            {
                Enabled = false,

            };

            _trayContextMenu.MenuItems.Add(_disableAppMenuItem);
            _trayContextMenu.MenuItems.Add(_forceTaskbarStateMenuItem);
            //TODO KTOCN _trayContextMenu.MenuItems.Add(_keepTaskbarIfCursorNearMenuItem);
            _trayContextMenu.MenuItems.Add(_autoStartupMenuItem);
            _trayContextMenu.MenuItems.Add(helpMenuItem);
            _trayContextMenu.MenuItems.Add(quitMenuItem);
            _trayContextMenu.MenuItems.Add("-");
            _trayContextMenu.MenuItems.Add(appNameMenuItem);

            _appSettingsNotifyIcon = new NotifyIcon(new ComponentContainer())
            {
                Icon = _applicationIcon,
                ContextMenu = _trayContextMenu,
                Text = Program.ApplicationName,
                Visible = true
            };
            
            Application.Run();
        }

        public static void SetIcon()
        {
            bool firstExecutionOfClickOnce = ApplicationDeployment.IsNetworkDeployed && ApplicationDeployment.CurrentDeployment.IsFirstRun;
            if (firstExecutionOfClickOnce)
            {
                RegistryKey allAppsUninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");

                string appUninstallSubKeyName = allAppsUninstallKey.GetSubKeyNames()
                    .Where(subKeyName => allAppsUninstallKey.OpenSubKey(subKeyName, true).GetValue("DisplayName")?.ToString() == Program.ApplicationName)
                    .FirstOrDefault();

                allAppsUninstallKey.OpenSubKey(appUninstallSubKeyName, true).SetValue("DisplayIcon", ApplicationDeployment.CurrentDeployment.DataDirectory + @"\AppIconRound.ico");
            }
        }

        private static void DisableApppMenuItem_Click(object sender, EventArgs args)
        {
            bool isAppDisabled = !_disableAppMenuItem.Checked;

            _disableAppMenuItem.Checked = isAppDisabled;

            Program.Disabled = isAppDisabled;
        }

        private static void AutoStartupMenuItem_Click(object sender, EventArgs args)
        {
            bool isAutoStartupEnabled = !Properties.Settings.Default.AutoStartup;
            Properties.Settings.Default.AutoStartup = isAutoStartupEnabled;

            _autoStartupMenuItem.Checked = isAutoStartupEnabled;

            SetAutoStartupState(isAutoStartupEnabled);
        }

        private static void SetAutoStartupState(bool autoStartupEnabled)
        {
            if (ApplicationDeployment.IsNetworkDeployed)
            {
                if (autoStartupEnabled)
                {
                    _startupAppsKey.SetValue(Program.ApplicationName, _applicationPath);
                }
                else if (_startupAppsKey.GetValue(Program.ApplicationName) != null)
                {
                    _startupAppsKey.DeleteValue(Program.ApplicationName);
                }
            }
        }

        private static void ForceStateMenuItem_Click(object sender, EventArgs args)
        {
            bool shouldForceTaskbarState = !Properties.Settings.Default.ForceTaskbarState;
            Properties.Settings.Default.ForceTaskbarState = shouldForceTaskbarState;

            _forceTaskbarStateMenuItem.Checked = shouldForceTaskbarState;
        }

        private static void KeepTaskbarCursorNearMenuItem_Click(object sender, EventArgs args)
        {
            bool shouldForceTaskbarState = !Properties.Settings.Default.ForceTaskbarState;
            Properties.Settings.Default.ForceTaskbarState = shouldForceTaskbarState;

            _forceTaskbarStateMenuItem.Checked = shouldForceTaskbarState;
        }

        private static void HelpMenuItem_Click(object sender, EventArgs args) => Process.Start(HelpLocation);

        private static void QuitMenuItem_Click(object sender, EventArgs args)
        {
            _appSettingsNotifyIcon.Dispose();
            _trayContextMenu.Dispose();
            Application.Exit();
        }

        private static void UpdateTaskbarVisibility()
        {
            _updateVisibilityTimer.Stop();

            bool isStartMenuVisible = StartMenu.GetCurrentVisibility();
            bool startMenuVisibilityChanged = isStartMenuVisible != _wasStartMenuVisibleBefore;

            bool wrongTaskbarVisibility = false;
            if (Properties.Settings.Default.ForceTaskbarState)
            {
                bool isTaskbarVisible = _taskbar.GetCurrentVisibility();
                wrongTaskbarVisibility = isTaskbarVisible != _shouldTaskbarBeVisible;
            }

            if (startMenuVisibilityChanged || wrongTaskbarVisibility)
            {
                SetTaskbarVisibility(isStartMenuVisible);
            }

            if (startMenuVisibilityChanged)
            {
                _wasStartMenuVisibleBefore = isStartMenuVisible;

                if (isStartMenuVisible)
                {
                    _taskbar.SetFocus();
                }
            }

            _updateVisibilityTimer.Start();
        }

        private static void SetTaskbarVisibility(bool visible)
        {
            _taskbar.SetVisibility(visible);
            _shouldTaskbarBeVisible = visible;

            Thread.Sleep(TaskbarVisibilityChangedTimeout);
        }
    }

    internal sealed class StartMenu
    {
        private static StartMenu s_startMenuInstance = new StartMenu();

        private static IAppVisibility s_appVisibility;

        private StartMenu() =>
            s_appVisibility = (IAppVisibility)Activator.CreateInstance(
                Type.GetTypeFromCLSID(new Guid("7E5FE3D9-985F-4908-91F9-EE19F9FD1514")));

        public static bool GetCurrentVisibility()
        {
            HRESULT isLauncherVisibleResult = s_appVisibility.IsLauncherVisible(out bool isStartMenuVisible);

            if (isLauncherVisibleResult != HRESULT.S_OK)
            {
                throw new Exception(isLauncherVisibleResult.ToString());
            }

            return isStartMenuVisible;
        }
    }

    internal sealed class Taskbar
    {
        private const string ClassName = "Shell_TrayWnd";

        private readonly IntPtr _taskbarHandle;

        public Taskbar()
        {
            this._taskbarHandle = User32.FindWindow(ClassName, null);

            var appBarData = APPBARDATA.NewInstance();

            IntPtr getTaskbarPosMessageResult = Shell32.SHAppBarMessage(Shell32.ABM_GETTASKBARPOS, ref appBarData);
            if (getTaskbarPosMessageResult == IntPtr.Zero)
            {
                throw new InvalidOperationException("Error fetching taskbar data.");
            }

            this.Position = (TaskbarPosition)appBarData.uEdge;
            this.Bounds = Rectangle.FromLTRB(appBarData.rc.Left, appBarData.rc.Top, appBarData.rc.Right, appBarData.rc.Bottom);

            appBarData.cbSize = APPBARDATA.GetSizeOfAPPBARDATAType();
            int taskbarState = Shell32.SHAppBarMessage(Shell32.ABM_GETSTATE, ref appBarData).ToInt32();

            this.IsAutoHideEnabled = (taskbarState & Shell32.ABS_AUTOHIDE) == Shell32.ABS_AUTOHIDE;
        }

        public Rectangle Bounds { get; private set; }

        public TaskbarPosition Position { get; private set; }

        public bool IsAutoHideEnabled { get; private set; }

        public void SetVisibility(bool showTaskbar) => User32.ShowWindow(this._taskbarHandle, showTaskbar ? User32.SW_SHOW : User32.SW_HIDE);

        public bool GetCurrentVisibility() => User32.IsWindowVisible(this._taskbarHandle);

        public void SetFocus() => User32.SetForegroundWindow(this._taskbarHandle);
    }
}