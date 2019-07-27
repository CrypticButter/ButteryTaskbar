using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrypticButter.ButteryTaskbar.Taskbar {
    internal static class TaskbarManager {
        private static List<Taskbar> taskbars;
        private static Taskbar primaryTaskbar = TaskbarFactory.GetPrimaryTaskbar();

        static TaskbarManager() {
            taskbars = TaskbarFactory.GetAllSecondaryTaskbars();
            taskbars.Add(primaryTaskbar);
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
    }
}
