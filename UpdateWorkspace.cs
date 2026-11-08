using System;
using System.IO;
using System.Threading;

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
            Directory.CreateDirectory(directory);
            return directory;
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
                    return true;
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                // Antivirus scanners can briefly retain the downloaded ZIP or extracted executable.
                if (attempt < 9) Thread.Sleep(100);
            }
            return false;
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
