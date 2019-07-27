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
        /// <summary>
        /// The milliseconds between each update of the taskbar's visibility
        /// </summary>
        private const int UpdateTaskbarVisibilityInterval = 20;

        private static bool disabled = false;

        /// <summary>
        /// Used to trigger the updating of the taskbar at regular intervals
        /// </summary>
        private static System.Timers.Timer updateVisibilityTimer;

        /// <summary>
        /// If the Start Menu was visible before re-checking
        /// </summary>
        private static bool? wasStartMenuVisibleBefore = null;

        /// <summary>
        /// Should the application hide the taskbar
        /// </summary>
        public static bool Disabled {
            get => disabled;
            set {
                disabled = value;
                if (disabled) {
                    updateVisibilityTimer.Stop();
                    TaskbarManager.SetAllVisibility(true);
                } else {
                    TaskbarManager.SetAllVisibility(false);
                    updateVisibilityTimer.Start();
                }
            }
        }

        private void AppStartup(object sender, StartupEventArgs args) {
            AppNotifyIcon.InstantiateNotifyIcon();

            var appMutex = new Mutex(true, FriendlyApplicationName, out bool newMutexCreated);

            if (!newMutexCreated) {
                AppNotifyIcon.DisplayNotificationMessage("Buttery Taskbar is already running!");
                Thread.Sleep(7000);
                Quit();
                return;
            }

            SetupIfFirstRun();

            updateVisibilityTimer = new System.Timers.Timer {
                Interval = UpdateTaskbarVisibilityInterval,
                AutoReset = false,
            };
            updateVisibilityTimer.Elapsed += (s, e) => UpdateTaskbarVisibility();
            updateVisibilityTimer.Start();
        }

        private static void SetupIfFirstRun() {
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
            updateVisibilityTimer?.Dispose();
            TaskbarManager.SetAllVisibility(true);

            AppNotifyIcon.Dispose();
        }

        private static void UpdateTaskbarVisibility() {
            updateVisibilityTimer.Stop();

            bool isStartMenuVisible = StartMenu.GetCurrentVisibility();
            bool startMenuVisibilityChanged = isStartMenuVisible != wasStartMenuVisibleBefore;

            bool shouldCorrectVisibility = ButteryTaskbar.Properties.Settings.Default.ForceTaskbarState;
            bool wrongPrimaryTaskbarVisibility = TaskbarManager.AnyViolatingVisibilityOf(isStartMenuVisible) && shouldCorrectVisibility;
            if (startMenuVisibilityChanged || wrongPrimaryTaskbarVisibility) {
                TaskbarManager.SetAllVisibility(isStartMenuVisible);
            }

            if (startMenuVisibilityChanged) {
                wasStartMenuVisibleBefore = isStartMenuVisible;

                if (isStartMenuVisible) {
                    TaskbarManager.SetFocusOnPrimary();
                }
            }

            updateVisibilityTimer.Start();
        }
    }
}
