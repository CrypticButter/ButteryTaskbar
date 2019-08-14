using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace CrypticButter.ButteryTaskbar {
    using CrypticButter.ButteryTaskbar.Taskbar;
    using System.Threading.Tasks;
    using static CrypticButter.ButteryTaskbar.GlobalValues;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// The milliseconds between each update of the taskbar's visibility
        /// </summary>
        private const int UpdateTaskbarVisibilityInterval = 20;

        private const int RefindTaskbarDelay = 300;

        /// <summary>
        /// If the Start Menu was visible before re-checking
        /// </summary>
        private static bool? wasStartMenuVisibleBefore = null;

        /// <summary>
        /// Used to trigger the updating of the taskbar at regular intervals
        /// </summary>
        private static System.Timers.Timer updateVisibilityTimer;

        private static bool disabled = false;

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

        private static MainWindow _window;

        public MainWindow() {
            var appMutex = new Mutex(true, FriendlyApplicationName, out bool newMutexCreated);
            if (!newMutexCreated) {
                AppNotifyIcon.DisplayNotificationMessage("Buttery Taskbar is already running!");
                Thread.Sleep(7000);
                App.Quit();
                return;
            }

            InitializeComponent();
            _window = this;
            Hide();
            VersionTextBlock.Text = $"Currently running version: {AppVersionNumber}";

            AppNotifyIcon.InstantiateNotifyIcon();

            App.SetupIfFirstRun();

            Updater.RestartRequested += (s, e) => App.Quit(true);
            Updater.CompletedWithoutUpdate += (s, e) => StartClock();

            Updater.Update();
        }

        public static async void SetUpdatesMessage(string message) {
            _window.CheckForUpdatesMessageTextBlock.Text = message;
            await Task.Delay(5000);
            _window.CheckForUpdatesButton.IsEnabled = true;
        }

        public static void ShowWindow() {
            _window.Show();
            _window.Activate();
        }

        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e) {
            this.CheckForUpdatesButton.IsEnabled = false;
            Updater.Update(forceUpdate: true);
        }

        protected override void OnClosing(CancelEventArgs e) {
            e.Cancel = true;

            Hide();
        }

        private static void StartClock() {
            updateVisibilityTimer = new System.Timers.Timer {
                Interval = UpdateTaskbarVisibilityInterval,
                AutoReset = false,
            };
            updateVisibilityTimer.Elapsed += (s, e) => UpdateTaskbarVisibility();
            updateVisibilityTimer.Start();
        }

        private static void UpdateTaskbarVisibility() {
            updateVisibilityTimer.Stop();

            if (TaskbarManager.DoesTaskbarExist()) {
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
            } else {
                Thread.Sleep(RefindTaskbarDelay);
                TaskbarManager.TryFindingTaskbars();
            }

            updateVisibilityTimer.Start();
        }
    }
}
