using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Translumo.Infrastructure.Constants
{
    public static class Global
    {
        public static string AppPath;

        public static string PythonPath;

        public static string PipPath;

        public static string ModelsPath;

        static Global()
        {
#if DEBUG
            AppPath = System.AppDomain.CurrentDomain.BaseDirectory;
#else
            AppPath = Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
#endif
            PythonPath = Path.Combine(AppPath, "Python");
            PipPath = Path.Combine(PythonPath, "Scripts/pip.exe");
            ModelsPath = Path.Combine(AppPath, "models");
        }

        /// <summary>
        /// ASCII-safe (8.3) variant of <see cref="PythonPath"/>. The embedded CPython interpreter
        /// can mangle a home path that contains characters outside the system ANSI codepage
        /// (e.g. Japanese/Russian text inside a Chinese-locale path). Using the DOS 8.3 short path
        /// strips every non-ASCII character. Falls back to the original path when 8.3 name
        /// generation is disabled on the volume (the caller also forces PYTHONUTF8=1 as a second
        /// line of defence).
        /// </summary>
        public static string PythonPathShort => ToShortPath(PythonPath);

        /// <summary>
        /// ASCII-safe variant of <see cref="ModelsPath"/>, passed to EasyOCR's model storage
        /// directory for the same reason as <see cref="PythonPathShort"/>.
        /// </summary>
        public static string ModelsPathShort => ToShortPath(ModelsPath);

        private static string ToShortPath(string longPath)
        {
            if (string.IsNullOrEmpty(longPath))
            {
                return longPath;
            }

            // 8.3 names only exist for existing files/directories; otherwise keep the long path.
            if (!Directory.Exists(longPath) && !File.Exists(longPath))
            {
                return longPath;
            }

            var sb = new StringBuilder(1024);
            int length = GetShortPathName(longPath, sb, sb.Capacity);
            if (length > 0 && length <= sb.Capacity)
            {
                return sb.ToString();
            }

            return longPath;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, int cchBuffer);

        public static Version GetVersion()
        {
            return new Version(FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).ProductVersion);
        }
    }
}
