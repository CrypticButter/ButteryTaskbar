using System.ComponentModel;
using System.Threading;
using System.Windows;

namespace CrypticButter.ButteryTaskbar
{
    using System.Threading.Tasks;
    using static CrypticButter.ButteryTaskbar.GlobalValues; 

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow _window;

        public MainWindow()
        {
            InitializeComponent();
            _window = this;
            Hide();

            VersionTextBlock.Text = $"Currently running version: {AppVersionNumber}";

            Updater.Update();
        }

        public static async void SetUpdatesMessage(string message)
        {
            _window.CheckForUpdatesMessageTextBlock.Text = message;
            await Task.Delay(5000);
            _window.CheckForUpdatesButton.IsEnabled = true;
        }

        public static void ShowWindow()
        {
            _window.Show();
            _window.Activate();
        }

        private void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            this.CheckForUpdatesButton.IsEnabled = false;
            Updater.Update(forceUpdate: true);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            e.Cancel = true;

            Hide();
        }
    }
}
