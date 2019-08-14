namespace CrypticButter.ButteryTaskbar {
    using CrypticButter.ButteryTaskbar.Taskbar;
    using Microsoft.Win32;
    using System.Deployment.Application;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows;
    using static CrypticButter.ButteryTaskbar.GlobalValues;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {

        private void AppStartup(object sender, StartupEventArgs args) {

        }

        public static void SetupIfFirstRun() {
            bool firstExecutionOfClickOnce = IsDeployed && ApplicationDeployment.CurrentDeployment.IsFirstRun;
            if (firstExecutionOfClickOnce) {
                SetUninstallIconInRegistry();

                AppNotifyIcon.AutoStartup.SetAutoStartupState(ButteryTaskbar.Properties.Settings.Default.AutoStartup);
            }
        }

        private static void SetUninstallIconInRegistry() {
            RegistryKey allAppsUninstallKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");

            string appUninstallSubKeyName = allAppsUninstallKey.GetSubKeyNames()
                .Where(subKeyName => allAppsUninstallKey.OpenSubKey(subKeyName, true).GetValue("DisplayName")?.ToString() == FriendlyApplicationName)
                .FirstOrDefault();

            allAppsUninstallKey.OpenSubKey(appUninstallSubKeyName, true).SetValue("DisplayIcon", Path.Combine(System.Windows.Forms.Application.StartupPath, ApplicationIconFileName));
        }

        internal static void Quit(bool shouldRestart = false) {
            AppClosingCleanup();

            if (shouldRestart) {
                Process.Start(ResourceAssembly.Location);
            }
            Current.Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e) {
            AppClosingCleanup();
        }

        private static void AppClosingCleanup() {
            TaskbarManager.SetAllVisibility(true);

            AppNotifyIcon.Dispose();
        }
    }
}
