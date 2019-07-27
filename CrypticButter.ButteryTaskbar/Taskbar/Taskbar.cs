namespace CrypticButter.ButteryTaskbar.Taskbar {
    using CrypticButter.WindowsDesktopAPI;
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Win32Interop.WinHandles;

    public enum TaskbarPosition {
        Unknown = -1,
        Left,
        Top,
        Right,
        Bottom,
    }

    static class TaskbarFactory {
        private const string PrimaryClassName = "Shell_TrayWnd";
        private const string SecondaryClassName = "Shell_SecondaryTrayWnd";

        public static Taskbar GetPrimaryTaskbar() {
            var handle = TopLevelWindowUtils.FindWindow(wh => wh.GetClassName().Contains(PrimaryClassName));
            return new Taskbar(handle.RawPtr);
        }

        public static List<Taskbar> GetAllSecondaryTaskbars() {
            var allHandles = TopLevelWindowUtils.FindWindows(wh => wh.GetClassName().Contains(SecondaryClassName)).ToList();

            var allSecondaryTaskbars = new List<Taskbar>();
            allHandles.ForEach(x => allSecondaryTaskbars.Add(new Taskbar(x.RawPtr)));
            return allSecondaryTaskbars;
        }
    }

    class Taskbar {
        private IntPtr taskbarHandle;

        public Rectangle Bounds { get; private set; }
        public TaskbarPosition Position { get; private set; }
        public bool IsAutoHideEnabled { get; private set; }

        public void SetVisibility(bool shouldShowTaskbar) => User32.ShowWindow(this.taskbarHandle, shouldShowTaskbar ? User32.SW_SHOW : User32.SW_HIDE);
        public bool GetCurrentVisibility() => User32.IsWindowVisible(this.taskbarHandle);
        public void SetFocus() => User32.SetForegroundWindow(this.taskbarHandle);

        public Taskbar(IntPtr taskbarHandle) {
            this.taskbarHandle = taskbarHandle;

            var appBarData = APPBARDATA.NewInstance();

            IntPtr getTaskbarPosMessageResult = Shell32.SHAppBarMessage(Shell32.ABM_GETTASKBARPOS, ref appBarData);
            if (getTaskbarPosMessageResult == IntPtr.Zero) {
                throw new InvalidOperationException("Error fetching taskbar data.");
            }

            this.Position = (TaskbarPosition)appBarData.uEdge;
            this.Bounds = Rectangle.FromLTRB(appBarData.rc.Left, appBarData.rc.Top, appBarData.rc.Right, appBarData.rc.Bottom);

            appBarData.cbSize = APPBARDATA.GetSizeOfAPPBARDATAType();
            int taskbarState = Shell32.SHAppBarMessage(Shell32.ABM_GETSTATE, ref appBarData).ToInt32();

            this.IsAutoHideEnabled = (taskbarState & Shell32.ABS_AUTOHIDE) == Shell32.ABS_AUTOHIDE;
        }
    }
}