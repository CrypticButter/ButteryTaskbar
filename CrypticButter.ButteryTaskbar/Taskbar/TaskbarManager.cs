using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrypticButter.ButteryTaskbar.Taskbar {
    internal static class TaskbarManager {
        private static List<Taskbar> taskbars = new List<Taskbar>();
        private static Taskbar primaryTaskbar;

        static TaskbarManager() {
            TryFindingTaskbars();
        }

        private static void InitialiseTaskbars() {
            primaryTaskbar = TaskbarFactory.GetPrimaryTaskbar();
            taskbars = TaskbarFactory.GetAllSecondaryTaskbars();
            taskbars.Add(primaryTaskbar);
        }

        public static void TryFindingTaskbars() {
            try {
                InitialiseTaskbars();
            } catch { }
        }

        public static void SetAllVisibility(bool shouldBeVisible) {
            for (int i = 0; i < taskbars.Count; i++) {
                taskbars[i].SetVisibility(shouldBeVisible);
            }
        }

        public static bool AnyViolatingVisibilityOf(bool visibility) {
            bool isVisibilityWrong = false;
            for (int i = 0; i < taskbars.Count; i++) {
                if (taskbars[i].GetCurrentVisibility() != visibility) {
                    isVisibilityWrong = true;
                    break;
                }
            }
            return isVisibilityWrong;
        }

        public static void SetFocusOnPrimary() {
            primaryTaskbar.SetFocus();
        }

        public static bool DoesTaskbarExist() {
            bool exists = taskbars.Any() && !primaryTaskbar.HasInvalidHandle();
            return exists;
        }
    }
}
