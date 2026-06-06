using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace DropResize.Updater
{
    internal static class Program
    {
        private const int CopyAttempts = 30;
        private const int GracefulCloseTimeoutMilliseconds = 15000;

        private static int Main(string[] args)
        {
            if (args.Length < 4)
            {
                return 1;
            }

            int processId;
            if (!int.TryParse(args[0], out processId))
            {
                return 1;
            }

            var sourceDirectory = args[1];
            var targetDirectory = args[2];
            var executablePath = args[3];

            if (!Directory.Exists(sourceDirectory) || !Directory.Exists(targetDirectory))
            {
                return 1;
            }

            try
            {
                CloseMainProcess(processId);
                CopyDirectory(sourceDirectory, targetDirectory);
                StartApplication(executablePath);
                TryDeleteDirectory(Path.GetDirectoryName(sourceDirectory));
                return 0;
            }
            catch
            {
                return 1;
            }
        }

        private static void CloseMainProcess(int processId)
        {
            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        process.CloseMainWindow();
                    }

                    if (!process.WaitForExit(GracefulCloseTimeoutMilliseconds) && !process.HasExited)
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
            }
            catch (ArgumentException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            foreach (var directory in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(sourceDirectory, directory);
                Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
            }

            foreach (var file in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = GetRelativePath(sourceDirectory, file);
                var destination = Path.Combine(targetDirectory, relativePath);
                var destinationDirectory = Path.GetDirectoryName(destination);

                if (!string.IsNullOrEmpty(destinationDirectory))
                {
                    Directory.CreateDirectory(destinationDirectory);
                }

                CopyFileWithRetry(file, destination);
            }
        }

        private static void CopyFileWithRetry(string source, string destination)
        {
            for (var attempt = 1; attempt <= CopyAttempts; attempt++)
            {
                try
                {
                    File.Copy(source, destination, true);
                    return;
                }
                catch (IOException)
                {
                    if (attempt == CopyAttempts)
                    {
                        throw;
                    }

                    Thread.Sleep(500);
                }
                catch (UnauthorizedAccessException)
                {
                    if (attempt == CopyAttempts)
                    {
                        throw;
                    }

                    Thread.Sleep(500);
                }
            }
        }

        private static string GetRelativePath(string root, string path)
        {
            var rootUri = new Uri(AppendDirectorySeparator(root));
            var pathUri = new Uri(path);
            return Uri.UnescapeDataString(rootUri.MakeRelativeUri(pathUri).ToString())
                .Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static void StartApplication(string executablePath)
        {
            if (!File.Exists(executablePath))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                WorkingDirectory = Path.GetDirectoryName(executablePath),
                UseShellExecute = true
            });
        }

        private static void TryDeleteDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                return;
            }

            try
            {
                Directory.Delete(path, true);
            }
            catch
            {
            }
        }
    }
}
