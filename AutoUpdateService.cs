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
        private readonly HttpClient downloadClient;
        private readonly Action<string, string, string, string, string> startUpdater;
        private readonly Func<string> getUpdaterPath;

        public AutoUpdateService() : this(Client, StartUpdater,
            () => EmbeddedExecutableCache.GetExecutablePath(UpdaterResourceName)) { }

        internal AutoUpdateService(HttpClient downloadClient,
            Action<string, string, string, string, string> startUpdater, Func<string> getUpdaterPath)
        {
            this.downloadClient = downloadClient;
            this.startUpdater = startUpdater;
            this.getUpdaterPath = getUpdaterPath;
        }

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
            cancellationToken.ThrowIfCancellationRequested();
            var updateRoot = UpdateWorkspace.Create(updateInfo.Version);
            var packagePath = Path.Combine(updateRoot, "DropResize-Windows.zip");
            var extractPath = Path.Combine(updateRoot, "package");
            var updaterStarted = false;

            try
            {
                statusChanged?.Invoke("Downloading update...");
                await DownloadFileAsync(updateInfo.DownloadUrl, packagePath, cancellationToken).ConfigureAwait(false);

                cancellationToken.ThrowIfCancellationRequested();
                statusChanged?.Invoke("Preparing update...");
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(extractPath);
                ZipFile.ExtractToDirectory(packagePath, extractPath);
                cancellationToken.ThrowIfCancellationRequested();

                var sourceExecutable = FindPackagedExecutable(extractPath);
                var sourceDirectory = Path.GetDirectoryName(sourceExecutable);
                var currentExecutable = Assembly.GetExecutingAssembly().Location;
                var targetDirectory = Path.GetDirectoryName(currentExecutable);
                var updaterPath = getUpdaterPath();

                statusChanged?.Invoke("Restarting to apply update...");
                cancellationToken.ThrowIfCancellationRequested();
                startUpdater(updaterPath, sourceDirectory, targetDirectory, currentExecutable, updateRoot);
                // The updater now owns these files and removes the ZIP and extraction tree in finally.
                updaterStarted = true;

                return new AutoUpdateStartResult
                {
                    UpdaterPath = updaterPath,
                    SourceDirectory = sourceDirectory,
                    TargetDirectory = targetDirectory
                };
            }
            finally
            {
                if (!updaterStarted) UpdateWorkspace.TryDelete(updateRoot);
            }
        }

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };
        }

        private async Task DownloadFileAsync(string url, string destination, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "DropResize");

                using (var response = await downloadClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
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

        private static void StartUpdater(
            string updaterPath,
            string sourceDirectory,
            string targetDirectory,
            string executablePath,
            string updateRoot)
        {
            var currentProcess = Process.GetCurrentProcess();
            var arguments =
                currentProcess.Id + " " +
                Quote(sourceDirectory) + " " +
                Quote(targetDirectory) + " " +
                Quote(executablePath) + " " +
                Quote(updateRoot);

            using (var updater = Process.Start(new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = arguments,
                WorkingDirectory = Path.GetDirectoryName(updaterPath),
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (updater == null) throw new InvalidOperationException("The update helper could not be started.");
            }
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
