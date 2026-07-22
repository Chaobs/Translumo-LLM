using System;
using System.Diagnostics;
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
        /// Located at <c>&lt;executable directory&gt;/config</c>.
        /// </summary>
        public static string GetConfigDirectory()
        {
            var dir = Path.Combine(GetAppDirectory(), "config");
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

        /// <summary>
        /// Returns the directory that contains the running executable.
        /// </summary>
        /// <remarks>
        /// For a single-file published .NET app, <see cref="AppContext.BaseDirectory"/> points at the
        /// <c>%TEMP%/.net/&lt;app&gt;/&lt;hash&gt;</c> extraction directory rather than the real
        /// deployment folder, so we resolve the executable path from the current process' main module
        /// instead. This keeps the <c>config</c> folder next to the .exe where the user expects it.
        /// </remarks>
        private static string GetAppDirectory()
        {
            try
            {
                var mainModule = Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(mainModule))
                {
                    var dir = Path.GetDirectoryName(mainModule);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        return dir;
                    }
                }
            }
            catch
            {
                // Fall through to AppContext.BaseDirectory.
            }

            return AppContext.BaseDirectory;
        }
    }
}
