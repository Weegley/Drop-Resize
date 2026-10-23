using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace DropResize
{
    public sealed class WorkerProcessPool
    {
        public const int MaximumWorkerCount = 32;
        private const long BytesPerMegabyte = 1024L * 1024;
        private readonly string workerExecutablePath;

        public WorkerProcessPool(string workerExecutablePath)
        {
            this.workerExecutablePath = workerExecutablePath;
        }

        public async Task ProcessAsync(
            IList<string> inputFiles, ResizeProfile profile, string outputFolder, int requestedWorkers,
            CancellationToken cancellationToken, Action<ResizeResult> resultReceived,
            Action<int, int> progressChanged)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (inputFiles.Count == 0)
            {
                return;
            }

            var workerBatches = await Task.Run(() => CreateWorkerBatches(inputFiles, profile, requestedWorkers, cancellationToken),
                cancellationToken).ConfigureAwait(false);
            var workerJobFolder = Path.Combine(outputFolder, ".workers-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workerJobFolder);
            try
            {
                var workerJobPaths = new List<string>();
                for (var i = 0; i < workerBatches.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var workerJobPath = Path.Combine(workerJobFolder, i + ".xml");
                    WorkerProtocol.SaveJob(workerJobPath, new WorkerJob
                    {
                        InputFiles = workerBatches[i], Profile = profile, OutputFolder = outputFolder
                    });
                    workerJobPaths.Add(workerJobPath);
                }

                var completedImages = 0;
                var progressLock = new object();
                Exception firstWorkerFailure = null;
                using (var stopWorkers = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    var tasks = workerJobPaths.Select((path, index) => Task.Run(async () =>
                    {
                        try
                        {
                            await ProcessWorkerBatchAsync(path, workerBatches[index].Count, stopWorkers.Token, result =>
                            {
                                lock (progressLock)
                                {
                                    stopWorkers.Token.ThrowIfCancellationRequested();
                                    resultReceived(result);
                                    progressChanged(++completedImages, inputFiles.Count);
                                }
                            }).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            if (!(ex is OperationCanceledException))
                            {
                                Interlocked.CompareExchange(ref firstWorkerFailure, ex, null);
                            }
                            stopWorkers.Cancel();
                            throw;
                        }
                    })).ToArray();

                    try
                    {
                        await Task.WhenAll(tasks).ConfigureAwait(false);
                    }
                    catch
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (firstWorkerFailure != null)
                        {
                            ExceptionDispatchInfo.Capture(firstWorkerFailure).Throw();
                        }
                        throw;
                    }
                }
            }
            finally
            {
                // Await every worker before deleting job files or allowing the next batch to replace outputs.
                try { Directory.Delete(workerJobFolder, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        public static int CalculateWorkerCount(int requestedWorkers, int imageCount, long estimatedWorkerBytes, ulong availableMemoryBytes)
        {
            if (imageCount <= 0) return 0;
            var memoryBudget = availableMemoryBytes / 2;
            var workerBytes = (ulong)Math.Max(128 * BytesPerMegabyte, estimatedWorkerBytes);
            var memoryLimit = Math.Max(1UL, memoryBudget / workerBytes);
            var requestedLimit = Math.Min(imageCount, Math.Min(MaximumWorkerCount, Math.Max(1, requestedWorkers)));
            return (int)Math.Min((ulong)requestedLimit, memoryLimit);
        }

        public static int GetAutomaticWorkerCount()
        {
            return Math.Max(1, Math.Min(MaximumWorkerCount, Environment.ProcessorCount - 1));
        }

        private static List<List<string>> CreateWorkerBatches(
            IList<string> files, ResizeProfile profile, int requested, CancellationToken cancellationToken)
        {
            var estimatedWorkerBytes = 128 * BytesPerMegabyte;
            var sizes = new List<KeyValuePair<string, long>>();
            foreach (var path in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                long fileSize = 1;
                try
                {
                    fileSize = Math.Max(1, new FileInfo(path).Length);
                    using (var stream = File.OpenRead(path))
                    using (var image = Image.FromStream(stream, false, false))
                    {
                        var outputSize = profile.CalculateOutputSize(image.Width, image.Height);
                        var outputPixels = (long)outputSize.Width * outputSize.Height;
                        // Allow for decoder buffers, destination pixels and GDI+ resampling intermediates.
                        var estimate = checked(((long)image.Width * image.Height + outputPixels) * 8 + 64 * BytesPerMegabyte);
                        estimatedWorkerBytes = Math.Max(estimatedWorkerBytes, estimate);
                    }
                }
                catch (Exception ex) when (ex is IOException || ex is ArgumentException ||
                    ex is UnauthorizedAccessException || ex is OutOfMemoryException || ex is OverflowException ||
                    ex is ExternalException)
                {
                    // Let the worker report invalid or inaccessible files as per-image errors.
                }
                sizes.Add(new KeyValuePair<string, long>(path, fileSize));
            }

            var memory = new MemoryStatus();
            memory.Length = (uint)Marshal.SizeOf(typeof(MemoryStatus));
            var available = GlobalMemoryStatusEx(ref memory) ? memory.AvailablePhysical : (ulong)(512 * BytesPerMegabyte);
            var count = CalculateWorkerCount(requested, files.Count, estimatedWorkerBytes, available);
            var workerBatches = Enumerable.Range(0, count).Select(_ => new List<string>()).ToList();
            var assignedFileBytes = new long[count];
            foreach (var file in sizes.OrderByDescending(item => item.Value))
            {
                var index = 0;
                for (var i = 1; i < count; i++)
                {
                    if (assignedFileBytes[i] < assignedFileBytes[index]) index = i;
                }
                workerBatches[index].Add(file.Key);
                assignedFileBytes[index] += file.Value;
            }
            return workerBatches;
        }

        private async Task ProcessWorkerBatchAsync(
            string workerJobPath, int expectedResults, CancellationToken cancellationToken, Action<ResizeResult> resultReceived)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = workerExecutablePath, Arguments = "\"" + workerJobPath + "\"",
                    UseShellExecute = false, CreateNoWindow = true,
                    RedirectStandardOutput = true, RedirectStandardError = true
                };
                var workerExited = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                process.EnableRaisingEvents = true;
                // A queued Exited callback may run after Dispose, including when Start fails.
                // Only signal here; read ExitCode while the Process is still owned by this method.
                process.Exited += (sender, args) => workerExited.TrySetResult(true);
                if (!process.Start()) throw new InvalidOperationException("Could not start image worker.");
                var standardError = process.StandardError.ReadToEndAsync();
                try
                {
                    try { process.PriorityClass = ProcessPriorityClass.BelowNormal; }
                    catch (Win32Exception) { }
                    catch (InvalidOperationException) { }

                    using (cancellationToken.Register(() => TryKill(process)))
                    {
                        var receivedResultCount = 0;
                        string line;
                        while ((line = await process.StandardOutput.ReadLineAsync().ConfigureAwait(false)) != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (line.StartsWith(WorkerProtocol.ResultPrefix, StringComparison.Ordinal))
                            {
                                if (++receivedResultCount > expectedResults) throw new InvalidDataException("Worker returned extra results.");
                                var result = WorkerProtocol.ReadResultLine(line);
                                // Successful callbacks take ownership of the preview; rejected results must be disposed here.
                                try { resultReceived(result); }
                                catch { result.Dispose(); throw; }
                            }
                        }
                        await workerExited.Task.ConfigureAwait(false);
                        var exitCode = process.ExitCode;
                        var errorText = await standardError.ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (exitCode != 0)
                        {
                            throw new InvalidOperationException(string.IsNullOrWhiteSpace(errorText)
                                ? "Worker exited with code " + exitCode + "." : errorText.Trim());
                        }
                        if (receivedResultCount != expectedResults)
                        {
                            throw new InvalidDataException("Worker exited before returning all image results.");
                        }
                    }
                }
                finally
                {
                    TryKill(process);
                    await workerExited.Task.ConfigureAwait(false);
                    await standardError.ConfigureAwait(false);
                }
            }
        }

        private static void TryKill(Process process)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            catch (Win32Exception) { }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatus
        {
            public uint Length;
            public uint MemoryLoad;
            public ulong TotalPhysical;
            public ulong AvailablePhysical;
            public ulong TotalPageFile;
            public ulong AvailablePageFile;
            public ulong TotalVirtual;
            public ulong AvailableVirtual;
            public ulong AvailableExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
    }
}
