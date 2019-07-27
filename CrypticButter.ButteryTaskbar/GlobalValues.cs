using System;
using System.Collections.Generic;
using System.Deployment.Application;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrypticButter.ButteryTaskbar
{
    internal static class GlobalValues
    {
        /// <summary>
        /// The name of the file of the application icon
        /// </summary>
        public const string ApplicationIconFileName = "AppIconRound.ico";

        /// <summary>
        /// The URL for the program's help/support webpage
        /// </summary>
        public const string HelpLocation = "https://github.com/CrypticButter/ButteryTaskbar/wiki/";

        /// <summary>
        /// User-friendly name of the application
        /// </summary>
        public const string FriendlyApplicationName = "Buttery Taskbar";

        public const sbyte HoursBetweenUpdateCheck = 20;

        public static readonly bool IsDeployed = ApplicationDeployment.IsNetworkDeployed;

        public static readonly string AppVersionNumber = IsDeployed ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString() : "Dev";

        public static MouseButtons PrimaryMouseButton => Properties.Settings.Default.IsLeftHanded ? MouseButtons.Right : MouseButtons.Left;
    }
}
