using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Serilog;

namespace Translumo.Utils
{
    /// <summary>
    /// Manages the temporary directories that the .NET single-file host
    /// (<c>PublishSingleFile=true</c>) extracts the bundled assemblies and
    /// native libraries into at startup.
    /// </summary>
    /// <remarks>
    /// When the app is published as a single file, the runtime host extracts
    /// everything into <c>%TEMP%/.net/&lt;app&gt;/&lt;hash&gt;</c> on Windows
    /// before managed code runs. The host only reclaims the directory of the
    /// *current* run on a graceful exit. Crashes, forced kills (Task Manager),
    /// and every new binary hash therefore leave orphaned folders behind that
    /// silently accumulate on the system (C:) drive.
    /// <para />
    /// This helper (1) removes stale extractions safely and (2) redirects
    /// future extractions next to the executable so they no longer consume C:.
    /// </remarks>
    public static class TempBundleCleaner
    {
        /// <summary>Environment variable honored by the .NET single-file host (read before <c>Main()</c>).</summary>
        internal const string ExtractBaseDirEnv = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";

        /// <summary>Sub-folder the host appends under the extraction base (matches the assembly name).</summary>
        internal const string AppBundleFolder = "Translumo-LLM";

        /// <summary>
        /// Removes single-file extraction directories that are (a) not the one this
        /// process is currently loaded from and (b) not written to within
        /// <paramref name="maxAge"/>. Directories locked by another running instance
        /// (or denied access) are skipped. Best-effort: every failure is logged and
        /// swallowed so startup is never blocked.
        /// </summary>
        /// <param name="maxAge">Minimum age of an orphaned directory before deletion (guards against deleting a running instance's dir).</param>
        public static void CleanStaleExtractions(TimeSpan maxAge)
        {
            try
            {
                // For a single-file app this is the extraction dir, e.g.
                // <base>/Translumo-LLM/<hash>/. Never delete the folder we are running from.
                var current = (AppContext.BaseDirectory ?? string.Empty)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (string.IsNullOrEmpty(current))
                {
                    return;
                }

                var bases = GetExtractionBases().Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var cutoff = DateTime.UtcNow - maxAge;

                foreach (var baseDir in bases)
                {
                    var appRoot = Path.Combine(baseDir, AppBundleFolder);
                    if (!Directory.Exists(appRoot))
                    {
                        continue;
                    }

                    foreach (var sub in Directory.EnumerateDirectories(appRoot))
                    {
                        var normalized = sub.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        if (string.Equals(normalized, current, StringComparison.OrdinalIgnoreCase))
                        {
                            continue; // The directory this process is loaded from – never touch.
                        }

                        try
                        {
                            // Recently written => most likely an actively running instance. Leave it alone.
                            if (Directory.GetLastWriteTimeUtc(sub) > cutoff)
                            {
                                continue;
                            }

                            Directory.Delete(sub, recursive: true);
                            Log.Logger?.Information("Removed stale single-file extraction dir: {Dir}", sub);
                        }
                        catch (Exception ex)
                        {
                            // Locked by another instance or access denied – skip safely so we never break a running app.
                            Log.Logger?.Debug("Skipped extraction dir {Dir}: {Reason}", sub, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Logger?.Warning(ex, "Stale single-file extraction cleanup failed");
            }
        }

        /// <summary>
        /// Redirects future single-file extractions to a writable <c>temp</c> folder
        /// next to the executable so they no longer consume the system (C:) drive.
        /// The host reads <c>DOTNET_BUNDLE_EXTRACT_BASE_DIR</c> before managed code
        /// runs, so this only affects the *next* launch.
        /// </summary>
        /// <remarks>
        /// The variable is stored as a user-level environment variable (inherited by
        /// all future launches of this user). The path is absolute on purpose:
        /// relative values crash the .NET host silently (dotnet/runtime#46811).
        /// Writability is probed before committing, so a read-only deployment
        /// (e.g. Program Files) harmlessly falls back to the default %TEMP% location,
        /// which <see cref="CleanStaleExtractions"/> still reaps.
        /// </remarks>
        public static void ConfigureLocalExtractionBase()
        {
            try
            {
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                var exeDir = string.IsNullOrEmpty(exePath) ? null : Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(exeDir) || !Directory.Exists(exeDir))
                {
                    return;
                }

                var target = Path.Combine(exeDir, "temp");
                Directory.CreateDirectory(target);

                // Probe write access before committing the variable.
                var probe = Path.Combine(target, ".write_test");
                File.WriteAllBytes(probe, Array.Empty<byte>());
                File.Delete(probe);

                var existing = Environment.GetEnvironmentVariable(ExtractBaseDirEnv, EnvironmentVariableTarget.User);
                if (!string.Equals(existing, target, StringComparison.OrdinalIgnoreCase))
                {
                    Environment.SetEnvironmentVariable(ExtractBaseDirEnv, target, EnvironmentVariableTarget.User);
                    Log.Logger?.Information("Future single-file extractions redirected to {Dir}", target);
                }
            }
            catch (Exception ex)
            {
                // Non-fatal: the default %TEMP% location is still cleaned by CleanStaleExtractions.
                Log.Logger?.Debug("Could not redirect extraction base dir: {Reason}", ex.Message);
            }
        }

        /// <summary>Enumerates every extraction base directory that may hold our bundle (explicit override + default %TEMP%/.net).</summary>
        private static System.Collections.Generic.IEnumerable<string> GetExtractionBases()
        {
            // 1) Explicit override (manually set, or previously set by ConfigureLocalExtractionBase).
            var custom = Environment.GetEnvironmentVariable(ExtractBaseDirEnv);
            if (!string.IsNullOrWhiteSpace(custom))
            {
                yield return custom.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }

            // 2) Default .NET location on Windows: %TEMP%/.net
            var temp = Environment.GetEnvironmentVariable("TEMP")
                       ?? Environment.GetEnvironmentVariable("TMP")
                       ?? Path.GetTempPath();
            if (!string.IsNullOrWhiteSpace(temp))
            {
                yield return Path.Combine(temp, ".net");
            }
        }
    }
}
