using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DropResize
{
    public class AutoUpdateService
    {
        private const string UpdaterResourceName = "Drop&Resize.Updater.exe";
        private const string ApplicationFileName = "Drop&Resize.exe";
        private static readonly HttpClient Client = CreateClient();

        public async Task<AutoUpdateStartResult> DownloadAndStartUpdateAsync(
            UpdateInfo updateInfo,
            Action<string> statusChanged,
            CancellationToken cancellationToken)
        {
            if (updateInfo == null)
            {
                throw new ArgumentNullException("updateInfo");
            }

            if (string.IsNullOrWhiteSpace(updateInfo.DownloadUrl))
            {
                throw new InvalidOperationException("Release does not contain a downloadable Windows package.");
            }

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var updateRoot = CreateUpdateRoot(updateInfo);
            var packagePath = Path.Combine(updateRoot, "DropResize-Windows.zip");
            var extractPath = Path.Combine(updateRoot, "package");

            statusChanged?.Invoke("Downloading update...");
            await DownloadFileAsync(updateInfo.DownloadUrl, packagePath, cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            statusChanged?.Invoke("Preparing update...");

            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            Directory.CreateDirectory(extractPath);
            ZipFile.ExtractToDirectory(packagePath, extractPath);

            var sourceExecutable = FindPackagedExecutable(extractPath);
            var sourceDirectory = Path.GetDirectoryName(sourceExecutable);
            var currentExecutable = Assembly.GetExecutingAssembly().Location;
            var targetDirectory = Path.GetDirectoryName(currentExecutable);
            var updaterPath = ExtractUpdater(updateRoot);

            statusChanged?.Invoke("Restarting to apply update...");
            StartUpdater(updaterPath, sourceDirectory, targetDirectory, currentExecutable);

            return new AutoUpdateStartResult
            {
                UpdaterPath = updaterPath,
                SourceDirectory = sourceDirectory,
                TargetDirectory = targetDirectory
            };
        }

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        private static string CreateUpdateRoot(UpdateInfo updateInfo)
        {
            var safeVersion = string.IsNullOrWhiteSpace(updateInfo.VersionText)
                ? DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                : updateInfo.VersionText.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_');

            var root = Path.Combine(Path.GetTempPath(), "DropResize", "updates", safeVersion);

            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            Directory.CreateDirectory(root);
            return root;
        }

        private static async Task DownloadFileAsync(string url, string destination, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "DropResize");

                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();

                    using (var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        var buffer = new byte[81920];
                        int read;
                        while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
                        {
                            await output.WriteAsync(buffer, 0, read, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private static string FindPackagedExecutable(string extractPath)
        {
            var executable = Directory.GetFiles(extractPath, ApplicationFileName, SearchOption.AllDirectories);

            if (executable.Length == 0)
            {
                throw new InvalidOperationException("Downloaded package does not contain " + ApplicationFileName + ".");
            }

            return executable[0];
        }

        private static string ExtractUpdater(string updateRoot)
        {
            var updaterPath = Path.Combine(updateRoot, UpdaterResourceName);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(UpdaterResourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("Updater resource is missing.");
                }

                using (var output = new FileStream(updaterPath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    stream.CopyTo(output);
                }
            }

            return updaterPath;
        }

        private static void StartUpdater(
            string updaterPath,
            string sourceDirectory,
            string targetDirectory,
            string executablePath)
        {
            var currentProcess = Process.GetCurrentProcess();
            var arguments =
                currentProcess.Id + " " +
                Quote(sourceDirectory) + " " +
                Quote(targetDirectory) + " " +
                Quote(executablePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(updaterPath),
                UseShellExecute = true
            });
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }

    public class AutoUpdateStartResult
    {
        public string UpdaterPath { get; set; }
        public string SourceDirectory { get; set; }
        public string TargetDirectory { get; set; }
    }
}
