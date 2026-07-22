using System;
using System.IO;
using Translumo.Utils;
using Xunit;

namespace Translumo.Tests.Utils
{
    public class TempBundleCleanerTests
    {
        [Fact]
        public void CleanStaleExtractions_RemovesOldOrphans_KeepsRecentAndCurrent()
        {
            // Arrange: use a custom extraction base so the test does not depend on the
            // real %TEMP%/.net state. ConfigureLocalExtractionBase would normally set this;
            // here we set the process-level override directly (read by GetExtractionBases).
            var baseDir = Path.Combine(Path.GetTempPath(), "TranslumoCleanerTest_" + Guid.NewGuid().ToString("N"));
            var appRoot = Path.Combine(baseDir, TempBundleCleaner.AppBundleFolder);
            Directory.CreateDirectory(appRoot);

            var oldDir = Path.Combine(appRoot, "hash_old");
            var recentDir = Path.Combine(appRoot, "hash_recent");
            Directory.CreateDirectory(oldDir);
            Directory.CreateDirectory(recentDir);
            File.WriteAllText(Path.Combine(oldDir, "a.dll"), "x");
            File.WriteAllText(Path.Combine(recentDir, "b.dll"), "x");

            // Force the old directory's write time into the past so it is considered stale.
            Directory.SetLastWriteTimeUtc(oldDir, DateTime.UtcNow.AddHours(-2));

            var prevEnv = Environment.GetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv);
            Environment.SetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv, baseDir);

            try
            {
                // Act
                TempBundleCleaner.CleanStaleExtractions(TimeSpan.FromHours(1));

                // Assert
                Assert.False(Directory.Exists(oldDir), "Stale extraction dir should have been removed");
                Assert.True(Directory.Exists(recentDir), "Recently written dir must be preserved");
            }
            finally
            {
                Environment.SetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv, prevEnv);
                try { Directory.Delete(baseDir, recursive: true); } catch { /* best-effort */ }
            }
        }

        [Fact]
        public void CleanStaleExtractions_NoBaseDir_DoesNotThrow()
        {
            // Arrange: point the base at a non-existent directory.
            var baseDir = Path.Combine(Path.GetTempPath(), "TranslumoCleanerTest_Missing_" + Guid.NewGuid().ToString("N"));
            var prevEnv = Environment.GetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv);
            Environment.SetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv, baseDir);

            try
            {
                // Act / Assert: must not throw even when nothing exists.
                var ex = Record.Exception(() => TempBundleCleaner.CleanStaleExtractions(TimeSpan.FromHours(1)));
                Assert.Null(ex);
            }
            finally
            {
                Environment.SetEnvironmentVariable(TempBundleCleaner.ExtractBaseDirEnv, prevEnv);
            }
        }

        [Fact]
        public void ConfigureLocalExtractionBase_ProbesAndSetsWritableTarget()
        {
            // Arrange: this test runs from the test host executable, whose directory is
            // writable, so ConfigureLocalExtractionBase should set the user-level env var
            // to "<exeDir>/temp". We capture the previous value and restore it afterwards.
            var prevEnv = Environment.GetEnvironmentVariable(
                TempBundleCleaner.ExtractBaseDirEnv, EnvironmentVariableTarget.User);

            try
            {
                // Act
                TempBundleCleaner.ConfigureLocalExtractionBase();

                // Assert: the redirect was applied and points next to the executable.
                var actual = Environment.GetEnvironmentVariable(
                    TempBundleCleaner.ExtractBaseDirEnv, EnvironmentVariableTarget.User);
                var exeDir = Path.GetDirectoryName(
                    System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty);

                Assert.Equal(Path.Combine(exeDir!, "temp"), actual);
                Assert.True(Directory.Exists(actual), "Redirected temp base must exist");
            }
            finally
            {
                Environment.SetEnvironmentVariable(
                    TempBundleCleaner.ExtractBaseDirEnv, prevEnv, EnvironmentVariableTarget.User);
            }
        }
    }
}
