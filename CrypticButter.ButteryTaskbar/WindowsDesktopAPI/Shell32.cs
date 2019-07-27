namespace CrypticButter.WindowsDesktopAPI
{
    using System;
    using System.Runtime.InteropServices;

    public static class Shell32
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
}