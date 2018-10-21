using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace CrypticButter.ButteryTaskbar
{
    public partial class MainForm : Form
    {
        /// <summary>
        /// The URL for the program's help/support webpage
        /// </summary>
        private const string HelpLocation = "https://github.com/CrypticButter/ButteryTaskbar/wiki/The-Buttery-Taskbar-Wiki";

        /// <summary>
        /// The name of the file of the application icon
        /// </summary>
        private const string ApplicationIconFileName = "AppIconRound.ico";

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
        private static string _applicationPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs) + @"\Cryptic Butter\Buttery Taskbar.appref-ms";

        public MainForm()
        {
            InitializeComponent();

            SetAutoStartupState(Properties.Settings.Default.AutoStartup);

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
            var appNameMenuItem = new MenuItem($"Buttery Taskbar {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}")
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

            _appSettingsNotifyIcon = new NotifyIcon
            {
                Icon = _applicationIcon,
                ContextMenu = _trayContextMenu,
                Text = "Buttery Taskbar",
                Visible = true
            };

            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
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
            if (autoStartupEnabled)
            {
                _startupAppsKey.SetValue("Buttery Taskbar", _applicationPath);
            }
            else if (_startupAppsKey.GetValue("Buttery Taskbar") != null)
            {
                _startupAppsKey.DeleteValue("Buttery Taskbar");
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
    }
}
