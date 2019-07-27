namespace CrypticButter.ButteryTaskbar
{
    using CrypticButter.WindowsDesktopAPI;
    using System;

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
}