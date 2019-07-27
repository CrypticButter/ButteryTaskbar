namespace CrypticButter.WindowsDesktopAPI
{
    using System;
    using System.Runtime.InteropServices;

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
}