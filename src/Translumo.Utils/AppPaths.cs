using System;
using System.IO;

namespace Translumo.Utils
{
    /// <summary>
    /// Resolves the directories used for mutable application data (LLM profiles, settings, ...).
    /// All configuration now lives under a portable <c>config</c> folder next to the executable
    /// instead of <c>%AppData%</c>, so a deployment can be copied/backed up as a single folder.
    /// </summary>
    public static class AppPaths
    {
        /// <summary>
        /// Directory that holds every mutable configuration file. It is created on first access.
        /// Located at <c>&lt;application base directory&gt;/config</c>.
        /// </summary>
        public static string GetConfigDirectory()
        {
            var dir = Path.Combine(AppContext.BaseDirectory, "config");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            return dir;
        }

        /// <summary>
        /// Legacy location used before the config-folder migration
        /// (<c>%AppData%/&lt;appName&gt;</c>). Used only as a one-time fallback when importing an
        /// existing configuration from an older build.
        /// </summary>
        public static string GetLegacyConfigDirectory()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appName = AppDomain.CurrentDomain.FriendlyName.Split('.')[0];
            return Path.Combine(appData, appName);
        }
    }
}
