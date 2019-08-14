using System;
using System.Deployment.Application;
using static CrypticButter.ButteryTaskbar.GlobalValues;

namespace CrypticButter.ButteryTaskbar
{
    internal static class Updater
    {
        public static event EventHandler<EventArgs> RestartRequested;
        public static event EventHandler<EventArgs> CompletedWithoutUpdate;

        public static void Update(bool forceUpdate = false)
        {
            if (IsDeployed)
            {
                MainWindow.SetUpdatesMessage("Checking for updates...");

                ApplicationDeployment.CurrentDeployment.CheckForUpdateCompleted += CheckForUpdateCompleted;
                ApplicationDeployment.CurrentDeployment.CheckForUpdateAsync();
            }
            else
            {
                MainWindow.SetUpdatesMessage("Error: app not deployed.");
                CompletedWithoutUpdate?.Invoke(new object(), new EventArgs());
            }
        }

        private static void CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                Properties.Settings.Default.LastCheckedForUpdate = DateTime.Now;
                Properties.Settings.Default.Save();

                if (e.UpdateAvailable && ApplicationDeployment.CurrentDeployment.CurrentVersion >= e.MinimumRequiredVersion)
                {
                    StartUpdate();
                }
                else
                {
                    MainWindow.SetUpdatesMessage($"Up to date as of {DateTime.Now.TimeOfDay.ToString()}.");
                    CompletedWithoutUpdate?.Invoke(new object(), new EventArgs());
                }
            }
            else
            {
                MainWindow.SetUpdatesMessage("Oh no, something went very very wrong.");
                CompletedWithoutUpdate?.Invoke(new object(), new EventArgs());
            }
        }

        private static void StartUpdate()
        {
            ApplicationDeployment.CurrentDeployment.UpdateCompleted += UpdateCompleted;
            ApplicationDeployment.CurrentDeployment.UpdateAsync();
        }

        private static void UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (!e.Cancelled)
            {
                if (e.Error == null)
                {
                    RestartRequested?.Invoke(new object(), new EventArgs());
                }
                else
                {
                    AppNotifyIcon.DisplayNotificationMessage($"We were unable to update to the latest version.\nError: {e.Error.Message}");
                    CompletedWithoutUpdate.Invoke(new object(), new EventArgs());
                }
            } else {
                CompletedWithoutUpdate?.Invoke(new object(), new EventArgs());
            }
        }
    }
}
