namespace CrypticButter.ButteryTaskbar {
    using Microsoft.Win32;
    using System;
    using System.Deployment.Application;
    using System.Diagnostics;
    using System.Drawing;
    using System.Windows.Forms;
    using static CrypticButter.ButteryTaskbar.GlobalValues;

    internal static class AppNotifyIcon {
        public static bool IsVisible {
            get => _appSettingsNotifyIcon.Visible;
            set => _appSettingsNotifyIcon.Visible = value;
        }

        /// <summary>
        /// The application icon for the system tray
        /// </summary>
        private static Icon _applicationIcon = new Icon(ApplicationIconFileName);

        /// <summary>
        /// The tray icon for the application, with access to settings
        /// </summary>
        private static NotifyIcon _appSettingsNotifyIcon = null;

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
        /// Tray menu item for disabling the functionality of the programme
        /// </summary>
        private static MenuItem _disableAppMenuItem;

        /// <summary>
        /// Gives the user the option to restat the app if an update is available
        /// </summary>
        private static MenuItem _restartMenuItem;

        internal static void Dispose() {
            _appSettingsNotifyIcon?.Dispose();
            _trayContextMenu?.Dispose();
        }

        internal static void DisplayNotificationMessage(string message, string title = FriendlyApplicationName) {
            InstantiateNotifyIcon();

            _appSettingsNotifyIcon.BalloonTipTitle = title;
            _appSettingsNotifyIcon.BalloonTipText = message;

            _appSettingsNotifyIcon.ShowBalloonTip(5000);
        }

        internal static void InstantiateNotifyIcon() {
            if (_appSettingsNotifyIcon == null) {
                _appSettingsNotifyIcon = NewSettingsAppNotifyIcon();
            }
        }

        private static NotifyIcon NewSettingsAppNotifyIcon() {
            // TODO enable
            /*
            var openSettingsMenuItem = new MenuItem("Open Settings", OpenSettingsMenuItem_Click)
            {
                DefaultItem = true,
            };*/
            _disableAppMenuItem = new MenuItem("Disable", DisableAppMenuItem_Click) {
                Checked = false,
            };
            _forceTaskbarStateMenuItem = new MenuItem("Keep taskbar hidden", ForceStateMenuItem_Click) {
                Checked = Properties.Settings.Default.ForceTaskbarState,
            };

            // TODO KTOCN
            /*_keepTaskbarIfCursorNearMenuItem = new MenuItem("Hide taskbar after cursor moves away", KeepTaskbarCursorNearMenuItem_Click)
            {
                Checked = Properties.Settings.Default.KeepTaskbarOpenIfCursorNear,
            };*/
            _autoStartupMenuItem = new MenuItem("Start with Windows", AutoStartupMenuItem_Click) {
                Checked = Properties.Settings.Default.AutoStartup,
                Enabled = IsDeployed,
            };

            var helpMenuItem = new MenuItem("Help", HelpMenuItem_Click);
            var cbWebsiteMenuItem = new MenuItem("Consume Butter", AuthorWebsiteMenuItem_Click);
            _restartMenuItem = new MenuItem("Restart program", RestartMenuItem_Click);
            var quitMenuItem = new MenuItem("Quit", QuitMenuItem_Click);
            var appNameMenuItem = new MenuItem($"{FriendlyApplicationName} {AppVersionNumber}") {
                Enabled = false,
            };

            //_trayContextMenu.MenuItems.Add(openSettingsMenuItem);
            //_trayContextMenu.MenuItems.Add("-");
            _trayContextMenu.MenuItems.Add(_disableAppMenuItem);
            _trayContextMenu.MenuItems.Add(_forceTaskbarStateMenuItem);

            // TODO KTOCN _trayContextMenu.MenuItems.Add(_keepTaskbarIfCursorNearMenuItem);
            _trayContextMenu.MenuItems.Add(_autoStartupMenuItem);
            _trayContextMenu.MenuItems.Add("-");
            _trayContextMenu.MenuItems.Add(helpMenuItem);
            _trayContextMenu.MenuItems.Add(cbWebsiteMenuItem);
            _trayContextMenu.MenuItems.Add(_restartMenuItem);
            _trayContextMenu.MenuItems.Add(quitMenuItem);
            _trayContextMenu.MenuItems.Add("-");
            _trayContextMenu.MenuItems.Add(appNameMenuItem);

            var newNotifyIcon = new NotifyIcon(new ComponentContainer()) {
                Icon = _applicationIcon,
                ContextMenu = _trayContextMenu,
                Text = FriendlyApplicationName,
                Visible = true
            };

            newNotifyIcon.MouseClick += AppNotifyIcon_MouseClick;

            return newNotifyIcon;
        }

        private static void AppNotifyIcon_MouseClick(object sender, MouseEventArgs e) {
            if (e.Button == PrimaryMouseButton) {
                MainWindow.ShowWindow();
            }
        }

        private static void OpenSettingsMenuItem_Click(object sender, EventArgs args) {
            MainWindow.ShowWindow();
        }

        private static void DisableAppMenuItem_Click(object sender, EventArgs args) {
            bool isAppDisabled = !_disableAppMenuItem.Checked;

            _disableAppMenuItem.Checked = isAppDisabled;

            MainWindow.Disabled = isAppDisabled;
        }

        private static void AutoStartupMenuItem_Click(object sender, EventArgs args) {
            bool isAutoStartupEnabled = !Properties.Settings.Default.AutoStartup;
            Properties.Settings.Default.AutoStartup = isAutoStartupEnabled;

            _autoStartupMenuItem.Checked = isAutoStartupEnabled;

            AutoStartup.SetAutoStartupState(isAutoStartupEnabled);
        }

        private static void ForceStateMenuItem_Click(object sender, EventArgs args) {
            bool shouldForceTaskbarState = !Properties.Settings.Default.ForceTaskbarState;
            Properties.Settings.Default.ForceTaskbarState = shouldForceTaskbarState;

            _forceTaskbarStateMenuItem.Checked = shouldForceTaskbarState;

            Properties.Settings.Default.Save();
        }

        // TODO KTOCN
        /*private static void KeepTaskbarCursorNearMenuItem_Click(object sender, EventArgs args)
        {
            bool cursorBasedAutoHideEnabled = !Properties.Settings.Default.KeepTaskbarOpenIfCursorNear;
            Properties.Settings.Default.KeepTaskbarOpenIfCursorNear = cursorBasedAutoHideEnabled;

            _keepTaskbarIfCursorNearMenuItem.Checked = cursorBasedAutoHideEnabled;

            Properties.Settings.Default.Save();
        }*/

        private static void HelpMenuItem_Click(object sender, EventArgs args) => Process.Start(HelpLocation);
        private static void AuthorWebsiteMenuItem_Click(object sender, EventArgs args) => Process.Start("https://crypticbutter.com/ref-butterytaskbar");
        private static void RestartMenuItem_Click(object sender, EventArgs args) => App.Quit(shouldRestart: true);
        private static void QuitMenuItem_Click(object sender, EventArgs args) => App.Quit();

        internal static class AutoStartup {
            private static RegistryKey _startupAppsKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            private static readonly string _applicationPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + $@"\Cryptic Butter\{FriendlyApplicationName}.appref-ms";

            internal static void SetAutoStartupState(bool autoStartupEnabled) {
                if (IsDeployed) {
                    if (autoStartupEnabled) {
                        _startupAppsKey.SetValue(FriendlyApplicationName, _applicationPath);
                    } else if (_startupAppsKey.GetValue(FriendlyApplicationName) != null) {
                        _startupAppsKey.DeleteValue(FriendlyApplicationName);
                    }

                    Properties.Settings.Default.Save();
                }
            }

        }
    }
}
