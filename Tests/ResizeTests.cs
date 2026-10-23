using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using DropResize;

internal static class ResizeTests
{
    private static string root;
    private static string worker;
    private static int passed;
    private static int failed;

    [STAThread]
    private static int Main(string[] args)
    {
        // Reuse this executable as a deliberately failing worker for protocol/lifecycle tests.
        if (args.Length == 1 && args[0].EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            return FakeWorker(args[0]);
        if (args.Length != 2) return 2;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle;
        root = Path.Combine(args[0], "tmp", "tests-" + Guid.NewGuid().ToString("N"));
        worker = args[1];
        Directory.CreateDirectory(root);

        Test("fit landscape and portrait, bounded previews", TestFit);
        Test("do not enlarge and explicit enlargement", TestEnlargement);
        Test("JPEG, PNG, BMP, GIF, TIFF inputs and all output formats", TestFormats);
        Test("PNG alpha and non-default DPI", TestAlphaAndDpi);
        Test("resampling matches previous code on opaque images", TestResampling);
        Test("JPEG quality is respected", TestJpegQuality);
        Test("corrupt and missing inputs return per-image errors", TestInvalidInputs);
        Test("source/output handles released after processing", TestFileHandles);
        Test("preview and XML-special metadata survive worker protocol", TestProtocol);
        Test("memory budget, file count and process ceiling", TestMemoryLimits);
        Test("main and worker prefer native architecture", TestArchitecture);
        Test("embedded worker matches the current worker build", TestEmbeddedWorker);
        Test("embedded tools are reused safely by concurrent app instances", TestExecutableCache);
        Test("pool streams previews and monotonic progress", TestPool);
        Test("same names across processes never overwrite outputs", TestDuplicateNames);
        Test("empty and pre-canceled batches start no work", TestEmptyAndCanceled);
        Test("cancel after first result waits for all workers", TestCancellation);
        Test("callback failure stops peers and cleans job files", TestCallbackFailure);
        Test("worker crash, missing executable and invalid protocol", TestWorkerFailures);
        Test("cards own small previews, dispose safely, tolerate missing preview", TestCards);

        Console.WriteLine("Passed: {0}; failed: {1}. Artifacts: {2}", passed, failed, root);
        return failed == 0 ? 0 : 1;
    }

    private static void Test(string name, Action body)
    {
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception ex) { failed++; Console.Error.WriteLine("FAIL " + name + ": " + ex); }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static string Folder()
    {
        var folder = Path.Combine(root, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static ResizeProfile Profile(OutputFormat format = OutputFormat.Png, int width = 160, int height = 160)
    {
        return new ResizeProfile { Id = "test", Name = "Test", MaxWidth = width, MaxHeight = height,
            DoNotEnlarge = true, OutputFormat = format, JpegQuality = 85, Suffix = "_resized" };
    }

    private static string Picture(int width = 640, int height = 480, ImageFormat format = null,
        string directory = null, string filename = "photo.png", bool alpha = false)
    {
        var path = Path.Combine(directory ?? Folder(), filename);
        using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
        {
            bitmap.SetResolution(144, 144);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(alpha ? Color.Transparent : Color.DarkBlue);
                using (var brush = new LinearGradientBrush(new Rectangle(0, 0, width, height),
                    alpha ? Color.FromArgb(128, 220, 70, 30) : Color.Orange, Color.Teal, 30))
                    graphics.FillRectangle(brush, width / 4, height / 4, width / 2, height / 2);
            }
            bitmap.Save(path, format ?? ImageFormat.Png);
        }
        return path;
    }

    private static ResizeResult Resize(string path, ResizeProfile profile, string output = null)
    {
        return new ImageResizeService().Resize(path, profile, output ?? Folder());
    }

    private static void CheckSuccess(ResizeResult result, int width, int height)
    {
        Assert(result.Success, result.ErrorMessage ?? "Missing output.");
        Assert(result.OutputWidth == width && result.OutputHeight == height, "Wrong result dimensions.");
        using (var image = Image.FromFile(result.OutputPath))
            Assert(image.Width == width && image.Height == height, "Wrong saved dimensions.");
        Assert(result.Thumbnail != null && result.Thumbnail.Width <= 96 && result.Thumbnail.Height <= 96,
            "Missing or oversized thumbnail.");
    }

    private static void TestFit()
    {
        foreach (var landscape in new[] { true, false })
        {
            var result = Resize(Picture(landscape ? 640 : 480, landscape ? 480 : 640), Profile());
            try { CheckSuccess(result, landscape ? 160 : 120, landscape ? 120 : 160); }
            finally { if (result.Thumbnail != null) result.Thumbnail.Dispose(); }
        }
    }

    private static void TestEnlargement()
    {
        var input = Picture(40, 20);
        var profile = Profile();
        var first = Resize(input, profile);
        try { CheckSuccess(first, 40, 20); } finally { first.Thumbnail.Dispose(); }
        profile.DoNotEnlarge = false;
        var second = Resize(input, profile);
        try { CheckSuccess(second, 160, 80); } finally { second.Thumbnail.Dispose(); }
    }

    private static void TestFormats()
    {
        var formats = new[] { ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.Tiff };
        foreach (var format in formats)
        {
            var extension = format.Guid == ImageFormat.Jpeg.Guid ? ".jpeg" : "." + format.ToString().ToLowerInvariant();
            var input = Picture(80, 60, format, filename: "input" + extension);
            Assert(ImageResizeService.IsSupportedInputFile(input), "Supported input rejected.");
            foreach (OutputFormat output in Enum.GetValues(typeof(OutputFormat)))
            {
                var result = Resize(input, Profile(output));
                try
                {
                    CheckSuccess(result, 80, 60);
                    using (var saved = Image.FromFile(result.OutputPath))
                    {
                        var expected = output == OutputFormat.Png ? ImageFormat.Png :
                            output == OutputFormat.Bmp ? ImageFormat.Bmp : ImageFormat.Jpeg;
                        Assert(saved.RawFormat.Guid == expected.Guid, "Wrong output encoding.");
                    }
                }
                finally { if (result.Thumbnail != null) result.Thumbnail.Dispose(); }
            }
        }
        var jpg = Picture(80, 60, ImageFormat.Jpeg, filename: "input.jpg");
        Assert(ImageResizeService.IsSupportedInputFile(jpg), "JPG rejected.");
    }

    private static void TestAlphaAndDpi()
    {
        var result = Resize(Picture(alpha: true), Profile());
        try
        {
            CheckSuccess(result, 160, 120);
            using (var saved = new Bitmap(result.OutputPath))
            {
                Assert(saved.GetPixel(5, 5).A == 0, "Transparent background was lost.");
                var pixel = saved.GetPixel(60, 50);
                Assert(pixel.A > 120 && pixel.A < 250, "Partial transparency was lost.");
                Assert(Math.Abs(saved.HorizontalResolution - 144) < 1, "Source DPI was not preserved.");
            }
        }
        finally { result.Thumbnail.Dispose(); }
    }

    private static void TestResampling()
    {
        var input = Picture(640, 480);
        var result = Resize(input, Profile());
        try
        {
            CheckSuccess(result, 160, 120);
            using (var sourceImage = Image.FromFile(input))
            using (var oldSource = new Bitmap(sourceImage))
            using (var expected = new Bitmap(160, 120))
            using (var actual = new Bitmap(result.OutputPath))
            {
                using (var graphics = Graphics.FromImage(expected))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.Clear(Color.Transparent);
                    graphics.DrawImage(oldSource, 0, 0, 160, 120);
                }
                var maximumDifference = 0;
                for (var y = 0; y < 120; y++)
                for (var x = 0; x < 160; x++)
                {
                    var a = actual.GetPixel(x, y); var b = expected.GetPixel(x, y);
                    maximumDifference = Math.Max(maximumDifference, Math.Max(Math.Abs(a.R - b.R),
                        Math.Max(Math.Abs(a.G - b.G), Math.Abs(a.B - b.B))));
                }
                Assert(maximumDifference <= 2, "Resampling changed: max channel difference " + maximumDifference);
            }
        }
        finally { result.Thumbnail.Dispose(); }
    }

    private static void TestJpegQuality()
    {
        var input = Picture();
        var profile = Profile(OutputFormat.Jpeg, 640, 480);
        profile.JpegQuality = 10;
        var low = Resize(input, profile);
        profile.JpegQuality = 100;
        var high = Resize(input, profile);
        try
        {
            Assert(low.Success && high.Success, "JPEG save failed.");
            Assert(high.OutputFileSize > low.OutputFileSize, "Quality setting did not change JPEG size.");
        }
        finally { low.Thumbnail.Dispose(); high.Thumbnail.Dispose(); }
    }

    private static void TestInvalidInputs()
    {
        var broken = Path.Combine(Folder(), "broken.jpg");
        File.WriteAllText(broken, "not an image");
        foreach (var path in new[] { broken, broken + ".missing" })
        {
            var result = Resize(path, Profile());
            Assert(!result.Success && result.Status == "Error" && !string.IsNullOrEmpty(result.ErrorMessage),
                "Invalid input was accepted.");
        }
    }

    private static void TestFileHandles()
    {
        var input = Picture();
        var result = Resize(input, Profile());
        try
        {
            CheckSuccess(result, 160, 120);
            using (File.Open(input, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            using (File.Open(result.OutputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }
        finally { result.Thumbnail.Dispose(); }
    }

    private static void TestProtocol()
    {
        var original = Resize(Picture(), Profile());
        try
        {
            original.InputPath = "metadata & <xml> \r\n кириллица";
            var line = WorkerProtocol.FormatResultLine(original);
            Assert(!line.Contains("\r") && !line.Contains("\n"), "Wire result spans multiple lines.");
            var restored = WorkerProtocol.ReadResultLine(line);
            try
            {
                Assert(restored.InputPath == original.InputPath, "Metadata did not round-trip.");
                Assert(restored.Thumbnail.Width == original.Thumbnail.Width, "Preview did not round-trip.");
                Assert(restored.Success, "Success result was lost.");
            }
            finally { restored.Thumbnail.Dispose(); }
        }
        finally { original.Thumbnail.Dispose(); }
    }

    private static void TestMemoryLimits()
    {
        const long mb = 1024L * 1024;
        Assert(WorkerProcessPool.CalculateWorkerCount(8, 20, 256 * mb, (ulong)(1024 * mb)) == 2, "Memory limit ignored.");
        Assert(WorkerProcessPool.CalculateWorkerCount(8, 1, 128 * mb, ulong.MaxValue) == 1, "Idle workers launched.");
        Assert(WorkerProcessPool.CalculateWorkerCount(100, 100, 128 * mb, ulong.MaxValue) == 32, "Process ceiling ignored.");
        Assert(WorkerProcessPool.CalculateWorkerCount(8, 0, 1, 0) == 0, "Empty batch has workers.");
        Assert(WorkerProcessPool.CalculateWorkerCount(0, 2, 1, 0) == 1, "Low-memory single-worker fallback failed.");
    }

    private static void TestArchitecture()
    {
        foreach (var path in new[] { worker, typeof(WorkerProcessPool).Assembly.Location })
        {
            PortableExecutableKinds kind; ImageFileMachine machine;
            Assembly.ReflectionOnlyLoadFrom(path).ManifestModule.GetPEKind(out kind, out machine);
            Assert((kind & PortableExecutableKinds.Preferred32Bit) == 0, "Prefer32Bit is still set: " + path);
        }
    }

    private static List<ResizeResult> RunPool(IList<string> files, string output, ResizeProfile profile = null,
        Action<ResizeResult> callback = null, CancellationToken cancellation = default(CancellationToken),
        string executable = null)
    {
        var results = new List<ResizeResult>();
        var expectedProgress = 0;
        var task = new WorkerProcessPool(executable ?? worker).ProcessAsync(files, profile ?? Profile(), output, 2,
            cancellation, result =>
            {
                results.Add(result);
                try { if (callback != null) callback(result); }
                finally { result.Dispose(); }
            },
            (done, total) => Assert(done == ++expectedProgress && total == files.Count, "Progress is not monotonic."));
        if (!task.Wait(TimeSpan.FromSeconds(30))) throw new Exception("Worker pool timed out.");
        task.GetAwaiter().GetResult();
        Assert(results.Count == files.Count, "Results were lost.");
        Assert(!Directory.GetDirectories(output, ".workers-*").Any(), "Job folder was leaked.");
        return results;
    }

    private static void TestEmbeddedWorker()
    {
        using (var embedded = typeof(WorkerProcessPool).Assembly.GetManifestResourceStream("Drop&Resize.Worker.exe"))
        using (var built = File.OpenRead(worker))
        using (var hash = SHA256.Create())
        {
            Assert(embedded != null, "Worker resource is missing.");
            var embeddedHash = hash.ComputeHash(embedded);
            var builtHash = hash.ComputeHash(built);
            Assert(embeddedHash.SequenceEqual(builtHash), "Main app embeds a stale worker.");
        }
    }

    private static void TestExecutableCache()
    {
        var cacheFolder = Folder();
        var paths = new string[4];
        Parallel.For(0, paths.Length, index => paths[index] =
            EmbeddedExecutableCache.GetExecutablePath("Drop&Resize.Worker.exe", cacheFolder));
        Assert(paths.Distinct().Count() == 1, "Concurrent extraction used different files.");
        using (File.Open(paths[0], FileMode.Open, FileAccess.Read, FileShare.Read))
            Assert(EmbeddedExecutableCache.GetExecutablePath("Drop&Resize.Worker.exe", cacheFolder) == paths[0],
                "A running executable was overwritten.");
        Assert(!Directory.GetFiles(cacheFolder, "*.tmp", SearchOption.AllDirectories).Any(), "Extraction temp file leaked.");
    }

    private static void TestPool()
    {
        var broken = Path.Combine(Folder(), "bad.jpg"); File.WriteAllText(broken, "bad");
        var files = new[] { Picture(), Picture(480, 640), broken, Picture(20, 10) };
        var results = RunPool(files, Folder(), callback: result =>
        {
            if (result.Status != "Done") return;
            var restored = result;
            try { Assert(restored.Thumbnail != null && restored.Thumbnail.Width <= 96, "Pool omitted preview."); }
            finally { if (restored.Thumbnail != null) restored.Thumbnail.Dispose(); }
        });
        Assert(results.Count(result => result.Status == "Done") == 3, "Healthy files failed.");
        Assert(results.Count(result => result.Status == "Error") == 1, "Broken file was not reported.");
    }

    private static void TestDuplicateNames()
    {
        var files = Enumerable.Range(0, 8).Select(_ => Picture(filename: "same.png")).ToArray();
        var output = Folder();
        var existing = Path.Combine(output, "same_resized.png"); File.WriteAllText(existing, "keep me");
        var results = RunPool(files, output);
        Assert(results.All(result => result.Status == "Done"), "A duplicate-name image failed.");
        Assert(results.Select(result => result.OutputPath).Distinct(StringComparer.OrdinalIgnoreCase).Count() == files.Length,
            "Two results share a path.");
        Assert(File.ReadAllText(existing) == "keep me", "Pre-existing output was overwritten.");
    }

    private static void TestEmptyAndCanceled()
    {
        RunPool(new string[0], Folder());
        using (var cancel = new CancellationTokenSource())
        {
            cancel.Cancel();
            ExpectFailure<OperationCanceledException>(() => new WorkerProcessPool(worker).ProcessAsync(
                new[] { Picture() }, Profile(), Folder(), 2, cancel.Token, _ => { }, (done, total) => { }).GetAwaiter().GetResult());
        }
    }

    private static void TestCancellation()
    {
        var input = Picture();
        var output = Folder();
        var received = 0;
        using (var cancel = new CancellationTokenSource())
        {
            var task = new WorkerProcessPool(worker).ProcessAsync(Enumerable.Repeat(input, 12).ToArray(),
                Profile(), output, 2, cancel.Token, result => { result.Dispose(); received++; cancel.Cancel(); }, (done, total) => { });
            ExpectFailure<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }
        Assert(received == 1, "Callbacks continued after cancellation.");
        Assert(!Directory.GetDirectories(output, ".workers-*").Any(), "Canceled job folder remains.");
        foreach (var path in Directory.GetFiles(output))
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
    }

    private static void TestCallbackFailure()
    {
        var input = Picture(); var output = Folder();
        var task = new WorkerProcessPool(worker).ProcessAsync(Enumerable.Repeat(input, 8).ToArray(), Profile(),
            output, 2, CancellationToken.None, _ => { throw new ApplicationException("callback failure"); },
            (done, total) => { });
        ExpectFailure<ApplicationException>(() => task.GetAwaiter().GetResult());
        Assert(!Directory.GetDirectories(output, ".workers-*").Any(), "Failed batch job folder remains.");
    }

    private static void TestWorkerFailures()
    {
        var files = new[] { Picture(), Picture() };
        foreach (var mode in new[] { "no-results", "malformed", "stderr-failure", "extra-results" })
        {
            var profile = Profile(); profile.Id = mode;
            var output = Folder();
            var task = new WorkerProcessPool(typeof(ResizeTests).Assembly.Location).ProcessAsync(files, profile,
                output, 2, CancellationToken.None, _ => { }, (done, total) => { });
            Assert(Task.WhenAny(task, Task.Delay(15000)).GetAwaiter().GetResult() == task, "Failure path deadlocked.");
            ExpectFailure<Exception>(() => task.GetAwaiter().GetResult());
            Assert(!Directory.GetDirectories(output, ".workers-*").Any(), "Failed worker job folder remains.");
        }
        var missing = new WorkerProcessPool(Path.Combine(root, "missing.exe")).ProcessAsync(files, Profile(),
            Folder(), 2, CancellationToken.None, _ => { }, (done, total) => { });
        ExpectFailure<System.ComponentModel.Win32Exception>(() => missing.GetAwaiter().GetResult());
    }

    private static int FakeWorker(string jobPath)
    {
        WorkerJob job;
        using (var stream = File.OpenRead(jobPath)) job = (WorkerJob)new XmlSerializer(typeof(WorkerJob)).Deserialize(stream);
        if (job.Profile.Id == "stderr-failure")
        {
            Console.Error.Write(new string('x', 100000)); return 7;
        }
        if (job.Profile.Id == "malformed") Console.WriteLine("RESULT invalid-base64");
        if (job.Profile.Id == "extra-results")
        {
            for (var i = 0; i <= job.InputFiles.Count; i++)
                Console.WriteLine(WorkerProtocol.FormatResultLine(new ResizeResult { InputPath = job.InputFiles[0],
                    ErrorMessage = "simulated error", Status = "Error" }));
        }
        return 0;
    }

    private static void TestCards()
    {
        var result = Resize(Picture(), Profile());
        using (var card = new ImageCardControl(result))
            Assert(result.Thumbnail.Width <= 96, "Card holds a full-size image.");
        Assert(result.Thumbnail == null, "Disposed card retained its preview.");
        using (var card = new ImageCardControl(new ResizeResult { InputPath = "missing", OutputPath = "missing" })) { }
    }

    private static void ExpectFailure<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }
}
