using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DropResize
{
    // Shared by the app and updater: only an individual download session may be deleted.
    internal static class UpdateWorkspace
    {
        internal static string UpdatesDirectory
        {
            get { return Path.Combine(Path.GetTempPath(), "DropResize", "updates"); }
        }

        internal static string Create(Version version)
        {
            // Separate sessions also allow two app instances to download the same release safely.
            var name = (version == null ? "update" : version.ToString()) + "_" + Guid.NewGuid().ToString("N");
            var directory = Path.Combine(UpdatesDirectory, name);
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    return directory;
                }
                catch (DirectoryNotFoundException) when (attempt < 2)
                {
                    // Another updater may prune an empty parent between native directory-create calls.
                    Thread.Sleep(10);
                }
            }
        }

        internal static bool IsUpdateDirectory(string directory)
        {
            if (string.IsNullOrWhiteSpace(directory)) return false;
            try
            {
                var fullPath = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return string.Equals(Path.GetDirectoryName(fullPath), Path.GetFullPath(UpdatesDirectory),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (ArgumentException) { return false; }
            catch (NotSupportedException) { return false; }
            catch (IOException) { return false; }
        }

        internal static bool ContainsPath(string directory, string path)
        {
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(root, fullPath, StringComparison.OrdinalIgnoreCase) ||
                fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        internal static bool TryDelete(string directory)
        {
            if (!IsUpdateDirectory(directory)) return false;
            for (var attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    if (Directory.Exists(directory)) Directory.Delete(directory, true);
                    RemoveEmptyParentDirectories();
                    return true;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                // Antivirus scanners can briefly retain the downloaded ZIP or extracted executable.
                if (attempt < 9) Thread.Sleep(100);
            }
            return false;
        }

        private static void RemoveEmptyParentDirectories()
        {
            // Non-recursive deletion is atomic: an active download or unrelated file keeps its parent alive.
            if (TryDeleteEmptyDirectory(UpdatesDirectory))
                TryDeleteEmptyDirectory(Path.GetDirectoryName(UpdatesDirectory));
        }

        private static bool TryDeleteEmptyDirectory(string directory)
        {
            try { Directory.Delete(directory, false); return true; }
            catch (DirectoryNotFoundException) { return true; }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
        }

        internal static async Task CleanupAfterStartupAsync(Version installedVersion, string executablePath)
        {
            TryDeleteLegacyDownload(installedVersion, executablePath);
            // Older helpers finish deleting their download after starting the new application.
            await Task.Delay(1500).ConfigureAwait(false);
            TryDeleteLegacyDownload(installedVersion, executablePath);
        }

        internal static bool TryDeleteLegacyDownload(Version installedVersion, string executablePath)
        {
            if (installedVersion == null) return false;
            var directory = Path.Combine(UpdatesDirectory, installedVersion.ToString());
            // The old helper leaves its ZIP in a version-only folder. Cover the first upgrade too.
            if (ContainsPath(directory, executablePath)) return false;
            return TryDelete(directory);
        }
    }
}
