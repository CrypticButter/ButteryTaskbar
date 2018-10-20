namespace CrypticButter.ButteryTaskbar
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// Represents the taskbar
        /// </summary>
        private static Taskbar _taskbar = new Taskbar();

        /// <summary>
        /// Used to control the termination of the programme
        /// </summary>
        private static ManualResetEvent _terminateProgramEvent = new ManualResetEvent(false);

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

        private static void Main(string[] args)
        {
            _updateVisibilityTimer = new System.Timers.Timer
            {
                Interval = UpdateTaskbarVisibilityInterval,
                AutoReset = false,
            };
            _updateVisibilityTimer.Elapsed += (s, e) => UpdateTaskbarVisibility();
            _updateVisibilityTimer.Start();
            
            _terminateProgramEvent.WaitOne();
        }

        private static void UpdateTaskbarVisibility()
        {
            _updateVisibilityTimer.Stop();

            bool isStartMenuVisible = StartMenu.GetCurrentVisibility();
            bool startMenuVisibilityChanged = isStartMenuVisible != _wasStartMenuVisibleBefore;

            bool isTaskbarVisible = _taskbar.GetCurrentVisibility();
            bool wrongTaskbarVisibility = isTaskbarVisible != _shouldTaskbarBeVisible;

            if (startMenuVisibilityChanged || wrongTaskbarVisibility)
            {
                SetTaskbarVisibility(isStartMenuVisible);
            }

            if (startMenuVisibilityChanged)
            {
                _wasStartMenuVisibleBefore = isStartMenuVisible;
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
            var isLauncherVisibleResult = s_appVisibility.IsLauncherVisible(out bool isStartMenuVisible);

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

        private IntPtr _taskbarHandle;

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
    }
}