using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DropResize;

internal static class UpdateCleanupTests
{
    private static string artifactsRoot;
    private static string updaterExecutable;
    private static string testApplication;
    private static int versionSequence;

    public static void Initialize(string projectRoot, string testRoot, string workerPath)
    {
        artifactsRoot = testRoot;
        var configuration = new DirectoryInfo(Path.GetDirectoryName(workerPath)).Name;
        updaterExecutable = Path.Combine(projectRoot, "obj", "Updater", configuration, "Drop&Resize.Updater.exe");
        testApplication = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UpdateTestApplication.exe");
    }

    private sealed class DownloadHandler : HttpMessageHandler
    {
        private readonly Func<CancellationToken, HttpResponseMessage> respond;
        public int Requests;

        public DownloadHandler(Func<CancellationToken, HttpResponseMessage> respond) { this.respond = respond; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Requests++;
            token.ThrowIfCancellationRequested();
            return Task.FromResult(respond(token));
        }
    }

    private sealed class CanceledDownloadStream : MemoryStream
    {
        private readonly CancellationTokenSource cancellation;
        private bool hasRead;

        public CanceledDownloadStream(byte[] bytes, CancellationTokenSource cancellation) : base(bytes)
        {
            this.cancellation = cancellation;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken token)
        {
            if (hasRead)
            {
                cancellation.Cancel();
                return Task.FromCanceled<int>(token);
            }
            hasRead = true;
            return base.ReadAsync(buffer, offset, Math.Min(count, 64), token);
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static UpdateInfo Update()
    {
        var version = new Version(60000, Process.GetCurrentProcess().Id, 0, Interlocked.Increment(ref versionSequence));
        return new UpdateInfo { Version = version, VersionText = version.ToString(),
            DownloadUrl = "https://update.test.invalid/DropResize-Windows.zip" };
    }

    private static byte[] Package(bool includeExecutable = true, byte[] executable = null, string prefix = "DropResize/")
    {
        using (var stream = new MemoryStream())
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                using (var writer = new StreamWriter(archive.CreateEntry(prefix + "LICENSE.txt").Open()))
                    writer.Write("test license");
                if (includeExecutable)
                    using (var output = archive.CreateEntry(prefix + "Drop&Resize.exe").Open())
                    {
                        var bytes = executable ?? new byte[] { 1, 2, 3 };
                        output.Write(bytes, 0, bytes.Length);
                    }
            }
            return stream.ToArray();
        }
    }

    private static HttpClient Client(byte[] bytes, HttpStatusCode status = HttpStatusCode.OK)
    {
        return new HttpClient(new DownloadHandler(token => new HttpResponseMessage(status) {
            Content = new ByteArrayContent(bytes) }));
    }

    private static AutoUpdateService Service(HttpClient client,
        Action<string, string, string, string, string> launch = null, Func<string> resolve = null)
    {
        return new AutoUpdateService(client, launch ?? ((helper, source, target, executable, workspace) => {
            throw new Exception("The updater must not start for this test."); }), resolve ?? (() => "unused-helper.exe"));
    }

    private static HashSet<string> Downloads(UpdateInfo update)
    {
        return new HashSet<string>(Directory.Exists(UpdateWorkspace.UpdatesDirectory)
            ? Directory.GetDirectories(UpdateWorkspace.UpdatesDirectory, update.Version + "_*") : new string[0],
            StringComparer.OrdinalIgnoreCase);
    }

    private static Exception ExpectFailure(Action action)
    {
        try { action(); }
        catch (Exception exception) { return exception; }
        throw new Exception("Expected an update failure.");
    }

    public static void TestDownloadHandoff()
    {
        var update = Update();
        string workspace = null;
        using (var cancellation = new CancellationTokenSource())
        using (var client = Client(Package()))
        {
            try
            {
                var service = Service(client, (helper, source, target, executable, root) => {
                    workspace = root;
                    Assert(UpdateWorkspace.IsUpdateDirectory(root), "Helper received an unsafe workspace.");
                    Assert(UpdateWorkspace.ContainsPath(root, source), "Source is outside the cleanup folder.");
                    Assert(File.Exists(Path.Combine(root, "DropResize-Windows.zip")), "ZIP is missing at handoff.");
                    Assert(File.Exists(Path.Combine(source, "Drop&Resize.exe")), "Extracted executable is missing.");
                    Assert(!UpdateWorkspace.ContainsPath(root, target), "Installation is inside the cleanup folder.");
                    cancellation.Cancel(); // Once launched, cancellation must not delete the helper's source.
                });
                var result = service.DownloadAndStartUpdateAsync(update, null, cancellation.Token).GetAwaiter().GetResult();
                Assert(Directory.Exists(workspace) && Directory.Exists(result.SourceDirectory),
                    "The app deleted files already handed to the updater.");
            }
            finally { if (workspace != null) UpdateWorkspace.TryDelete(workspace); }
        }
    }

    private static void CheckFailure(byte[] bytes, HttpStatusCode status, Type expected,
        bool failLaunch = false, bool failResolution = false)
    {
        var update = Update();
        var before = Downloads(update);
        using (var client = Client(bytes, status))
        {
            var service = Service(client, failLaunch
                ? (Action<string, string, string, string, string>)((helper, source, target, executable, root) => {
                    throw new IOException("simulated launch failure"); }) : null,
                failResolution ? (Func<string>)(() => { throw new FileNotFoundException("helper missing"); }) : null);
            var error = ExpectFailure(() => service.DownloadAndStartUpdateAsync(update, null, CancellationToken.None)
                .GetAwaiter().GetResult());
            Assert(expected.IsInstanceOfType(error), "Unexpected update failure: " + error);
            Assert(Downloads(update).SetEquals(before), "Failed preparation retained a ZIP or extraction folder.");
        }
    }

    public static void TestPreparationFailures()
    {
        CheckFailure(new byte[0], HttpStatusCode.InternalServerError, typeof(HttpRequestException));
        CheckFailure(new byte[] { 1, 2, 3 }, HttpStatusCode.OK, typeof(InvalidDataException));
        CheckFailure(Package(false), HttpStatusCode.OK, typeof(InvalidOperationException));
        CheckFailure(Package(), HttpStatusCode.OK, typeof(IOException), failLaunch: true);
        CheckFailure(Package(), HttpStatusCode.OK, typeof(FileNotFoundException), failResolution: true);
        var update = Update();
        var before = Downloads(update);
        using (var client = Client(Package()))
        {
            ExpectFailure(() => Service(client).DownloadAndStartUpdateAsync(update,
                status => { throw new InvalidOperationException("status callback failed"); }, CancellationToken.None)
                .GetAwaiter().GetResult());
            Assert(Downloads(update).SetEquals(before), "A status callback failure retained downloaded files.");
        }
    }

    public static void TestDownloadCancellation()
    {
        foreach (var stage in new[] { "Downloading update...", "Preparing update...", "Restarting to apply update..." })
        using (var cancellation = new CancellationTokenSource())
        using (var client = Client(Package()))
        {
            var update = Update();
            var before = Downloads(update);
            var error = ExpectFailure(() => Service(client).DownloadAndStartUpdateAsync(update,
                status => { if (status == stage) cancellation.Cancel(); }, cancellation.Token).GetAwaiter().GetResult());
            Assert(error is OperationCanceledException, "Preparation did not respect cancellation at " + stage);
            Assert(Downloads(update).SetEquals(before), "Canceled preparation retained downloaded files.");
        }
        using (var cancellation = new CancellationTokenSource())
        using (var client = new HttpClient(new DownloadHandler(token => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StreamContent(new CanceledDownloadStream(Package(), cancellation)) })))
        {
            var update = Update();
            var before = Downloads(update);
            var error = ExpectFailure(() => Service(client).DownloadAndStartUpdateAsync(update, null, cancellation.Token)
                .GetAwaiter().GetResult());
            Assert(error is OperationCanceledException, "Partial download did not respect cancellation.");
            Assert(Downloads(update).SetEquals(before), "Canceled download retained its partially written ZIP.");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var handler = new DownloadHandler(token => { throw new Exception("Canceled request was sent."); });
            using (var client = new HttpClient(handler))
            {
                var error = ExpectFailure(() => Service(client).DownloadAndStartUpdateAsync(Update(), null, cancellation.Token)
                    .GetAwaiter().GetResult());
                Assert(error is OperationCanceledException && handler.Requests == 0, "Pre-cancellation started a download.");
            }
        }
    }

    public static void TestConcurrentDownloads()
    {
        var update = Update();
        var workspaces = new ConcurrentBag<string>();
        using (var client = Client(Package()))
        {
            try
            {
                var service = Service(client, (helper, source, target, executable, root) => workspaces.Add(root));
                Task.WhenAll(service.DownloadAndStartUpdateAsync(update, null, CancellationToken.None),
                    service.DownloadAndStartUpdateAsync(update, null, CancellationToken.None)).GetAwaiter().GetResult();
                Assert(workspaces.Count == 2 && workspaces.Distinct().Count() == 2, "Downloads reused a workspace.");
                Assert(workspaces.All(root => File.Exists(Path.Combine(root, "DropResize-Windows.zip"))),
                    "One download removed the other download's files.");
            }
            finally { foreach (var root in workspaces) UpdateWorkspace.TryDelete(root); }
        }
    }

    private static string Installation()
    {
        var directory = Path.Combine(artifactsRoot, "installed app & unicode-\u0442\u0435\u0441\u0442-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "settings.xml"), "keep user settings");
        return directory;
    }

    private static Process ExitedApplication()
    {
        var process = Process.Start(new ProcessStartInfo {
            FileName = testApplication, Arguments = "--exit", UseShellExecute = false, CreateNoWindow = true });
        Assert(process.WaitForExit(5000), "The old application fixture did not exit.");
        // Retain its process handle so Windows cannot reuse this PID during the updater test.
        return process;
    }

    private static int RunUpdater(int processId, string source, string target, string executable, string workspace)
    {
        var arguments = processId + " " + string.Join(" ", new[] { source, target, executable, workspace }
            .Select(value => "\"" + value + "\""));
        using (var updater = Process.Start(new ProcessStartInfo {
            FileName = updaterExecutable, Arguments = arguments, UseShellExecute = false, CreateNoWindow = true }))
        {
            if (!updater.WaitForExit(10000))
            {
                updater.Kill();
                updater.WaitForExit();
                throw new Exception("The updater did not finish.");
            }
            return updater.ExitCode;
        }
    }

    public static void TestInstallAndCleanup()
    {
        using (var embedded = typeof(AutoUpdateService).Assembly.GetManifestResourceStream("Drop&Resize.Updater.exe"))
        using (var expected = File.OpenRead(updaterExecutable))
        using (var hash = System.Security.Cryptography.SHA256.Create())
            Assert(hash.ComputeHash(embedded).SequenceEqual(hash.ComputeHash(expected)), "The embedded updater is stale.");

        foreach (var prefix in new[] { "DropResize/", "" })
        using (var oldApplication = ExitedApplication())
        using (var client = Client(Package(executable: File.ReadAllBytes(testApplication), prefix: prefix)))
        {
            var target = Installation();
            string workspace = null;
            try
            {
                var service = Service(client, (helper, source, ignoredTarget, ignoredExecutable, root) => {
                    workspace = root;
                    Assert(RunUpdater(oldApplication.Id, source, target, Path.Combine(target, "Drop&Resize.exe"), root) == 0,
                        "The real updater failed to install the package.");
                });
                service.DownloadAndStartUpdateAsync(Update(), null, CancellationToken.None).GetAwaiter().GetResult();
                Assert(!Directory.Exists(workspace), "Successful update retained its ZIP or extracted files.");
                Assert(File.ReadAllBytes(Path.Combine(target, "Drop&Resize.exe")).SequenceEqual(File.ReadAllBytes(testApplication)),
                    "The installed executable differs from the package.");
                var deadline = Stopwatch.StartNew();
                while (!File.Exists(Path.Combine(target, "restarted.txt")) && deadline.ElapsedMilliseconds < 5000)
                    Thread.Sleep(10);
                Assert(File.Exists(Path.Combine(target, "restarted.txt")), "The updated application did not restart.");
                Assert(File.ReadAllText(Path.Combine(target, "settings.xml")) == "keep user settings",
                    "Update cleanup modified user settings.");
            }
            finally { if (workspace != null) UpdateWorkspace.TryDelete(workspace); }
        }
    }

    public static void TestUpdaterFailures()
    {
        foreach (var failCopy in new[] { true, false })
        using (var oldApplication = ExitedApplication())
        {
            var workspace = UpdateWorkspace.Create(Update().Version);
            try
            {
                var source = Path.Combine(workspace, "package", "DropResize");
                Directory.CreateDirectory(source);
                File.WriteAllText(Path.Combine(workspace, "DropResize-Windows.zip"), "downloaded ZIP");
                File.WriteAllText(Path.Combine(source, "LICENSE.txt"), "license");
                var target = Installation();
                if (failCopy)
                {
                    Directory.CreateDirectory(Path.Combine(source, "assets"));
                    File.WriteAllText(Path.Combine(target, "assets"), "a file prevents directory creation");
                }
                Assert(RunUpdater(oldApplication.Id, source, target, Path.Combine(target, "missing-app.exe"), workspace) == 1,
                    "The updater did not report a copy/restart failure.");
                Assert(!Directory.Exists(workspace), "A failed update retained its ZIP or extracted files.");
                Assert(File.ReadAllText(Path.Combine(target, "settings.xml")) == "keep user settings", "Cleanup removed installed files.");
            }
            finally { UpdateWorkspace.TryDelete(workspace); }
        }
    }

    public static void TestCleanupBoundariesAndRetry()
    {
        var outside = Installation();
        Assert(!UpdateWorkspace.IsUpdateDirectory(UpdateWorkspace.UpdatesDirectory), "The entire updates root can be deleted.");
        Assert(!UpdateWorkspace.TryDelete(outside) && File.Exists(Path.Combine(outside, "settings.xml")),
            "Cleanup removed an unrelated folder.");
        var workspace = UpdateWorkspace.Create(Update().Version);
        try
        {
            var source = Path.Combine(workspace, "package");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "sentinel.txt"), "keep");
            using (var oldApplication = ExitedApplication())
            {
                Assert(RunUpdater(oldApplication.Id, source, source, Path.Combine(source, "app.exe"), workspace) == 1,
                    "Updater accepted installation inside the cleanup folder.");
                Assert(Directory.Exists(source), "Updater deleted its installation folder.");
                Assert(RunUpdater(oldApplication.Id, outside, outside, Path.Combine(outside, "app.exe"), workspace) == 1,
                    "Updater accepted a source outside the cleanup folder.");
                Assert(File.Exists(Path.Combine(source, "sentinel.txt")), "Rejected arguments removed staging files.");
            }
            Assert(!UpdateWorkspace.ContainsPath(workspace, workspace + "-other"), "A sibling folder counts as a child.");
            var zip = Path.Combine(workspace, "DropResize-Windows.zip");
            File.WriteAllText(zip, "downloaded ZIP");
            using (var locked = new FileStream(zip, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var release = Task.Run(() => { Thread.Sleep(250); locked.Dispose(); });
                Assert(UpdateWorkspace.TryDelete(workspace), "Cleanup did not retry a temporarily locked download.");
                release.GetAwaiter().GetResult();
            }
            Assert(!Directory.Exists(workspace), "Retry left the temporary download behind.");
        }
        finally { UpdateWorkspace.TryDelete(workspace); }
    }

    public static void TestEmptyParentCleanup()
    {
        var sandboxTemp = Path.Combine(artifactsRoot, "temp-parents-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(sandboxTemp);
        File.WriteAllText(Path.Combine(sandboxTemp, "keep-temp-root.txt"), "keep");
        var previousTmp = Environment.GetEnvironmentVariable("TMP");
        var previousTemp = Environment.GetEnvironmentVariable("TEMP");
        try
        {
            // Change only this test process; never prune the user's real shared TEMP folders in this test.
            Environment.SetEnvironmentVariable("TMP", sandboxTemp);
            Environment.SetEnvironmentVariable("TEMP", sandboxTemp);
            Assert(Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) ==
                Path.GetFullPath(sandboxTemp).TrimEnd(Path.DirectorySeparatorChar), "The test TEMP override was not applied.");
            var updatesDirectory = UpdateWorkspace.UpdatesDirectory;
            var applicationTemp = Path.GetDirectoryName(updatesDirectory);
            var first = UpdateWorkspace.Create(Update().Version);
            var active = UpdateWorkspace.Create(Update().Version);
            File.WriteAllText(Path.Combine(first, "download.zip"), "download");
            File.WriteAllText(Path.Combine(active, "download.zip"), "active download");
            Assert(UpdateWorkspace.TryDelete(first), "The finished download was not removed.");
            Assert(File.Exists(Path.Combine(active, "download.zip")) && Directory.Exists(updatesDirectory),
                "Parent cleanup removed another active download.");
            Assert(UpdateWorkspace.TryDelete(active), "The last download was not removed.");
            Assert(!Directory.Exists(updatesDirectory) && !Directory.Exists(applicationTemp),
                "Empty updates/DropResize parents remain after the last download.");

            var workspace = UpdateWorkspace.Create(Update().Version);
            var marker = Path.Combine(applicationTemp, "keep-other-data.txt");
            File.WriteAllText(marker, "other application data");
            Assert(UpdateWorkspace.TryDelete(workspace), "Download cleanup failed with other data in the app folder.");
            Assert(!Directory.Exists(updatesDirectory) && File.Exists(marker), "Cleanup removed unrelated data in DropResize.");
            File.Delete(marker);
            UpdateWorkspace.TryDeleteLegacyDownload(Update().Version, testApplication);
            Assert(!Directory.Exists(applicationTemp), "An empty DropResize folder without updates was not removed.");

            Parallel.For(0, 20, new ParallelOptions { MaxDegreeOfParallelism = 2 }, index => {
                var directory = UpdateWorkspace.Create(new Version(60000, 0, 0, index));
                File.WriteAllText(Path.Combine(directory, "download.zip"), "download");
                Assert(UpdateWorkspace.TryDelete(directory), "Concurrent cleanup failed.");
            });
            Assert(!Directory.Exists(applicationTemp), "Concurrent cleanup left empty parent folders.");

            var oldHelperVersion = Update().Version;
            var oldHelperWorkspace = UpdateWorkspace.Create(oldHelperVersion);
            File.WriteAllText(Path.Combine(oldHelperWorkspace, "download.zip"), "old helper is still cleaning up");
            var startupCleanup = UpdateWorkspace.CleanupAfterStartupAsync(oldHelperVersion, testApplication);
            Assert(Directory.Exists(oldHelperWorkspace), "Startup cleanup removed a helper's active workspace.");
            var oldHelperExit = Task.Run(() => {
                Thread.Sleep(100);
                Assert(UpdateWorkspace.IsUpdateDirectory(oldHelperWorkspace), "Invalid simulated helper cleanup path.");
                Directory.Delete(oldHelperWorkspace, true); // Older helpers do not remove empty parents.
            });
            Task.WhenAll(oldHelperExit, startupCleanup).GetAwaiter().GetResult();
            Assert(!Directory.Exists(applicationTemp), "Startup cleanup ran before the old helper finished and left empty parents.");
            Assert(File.Exists(Path.Combine(sandboxTemp, "keep-temp-root.txt")), "Cleanup removed the TEMP root itself.");
        }
        finally
        {
            Environment.SetEnvironmentVariable("TMP", previousTmp);
            Environment.SetEnvironmentVariable("TEMP", previousTemp);
        }
    }

    public static void TestLegacyCleanup()
    {
        var update = Update();
        var legacy = Path.Combine(UpdateWorkspace.UpdatesDirectory, update.Version.ToString());
        var active = UpdateWorkspace.Create(update.Version);
        Assert(!Directory.Exists(legacy), "The legacy test folder already exists.");
        Directory.CreateDirectory(legacy);
        try
        {
            File.WriteAllText(Path.Combine(legacy, "DropResize-Windows.zip"), "left by the old updater");
            Assert(!UpdateWorkspace.TryDeleteLegacyDownload(update.Version, Path.Combine(legacy, "Drop&Resize.exe")),
                "Legacy cleanup accepted an application running inside the temporary folder.");
            Assert(UpdateWorkspace.TryDeleteLegacyDownload(update.Version, Path.Combine(Installation(), "Drop&Resize.exe")),
                "The old helper's leftover ZIP was not removed.");
            Assert(!Directory.Exists(legacy) && Directory.Exists(active), "Legacy cleanup removed an active new download.");
        }
        finally { UpdateWorkspace.TryDelete(legacy); UpdateWorkspace.TryDelete(active); }
    }
}
