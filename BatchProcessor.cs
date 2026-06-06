using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DropResize
{
    public class BatchProcessor
    {
        private readonly ImageResizeService resizeService;

        public BatchProcessor(ImageResizeService resizeService)
        {
            this.resizeService = resizeService;
        }

        public async Task ProcessAsync(
            IEnumerable<string> inputFiles,
            ResizeProfile profile,
            string outputFolder,
            int workerCount,
            CancellationToken cancellationToken,
            Action<ResizeResult> resultReceived,
            Action<int, int> progressChanged)
        {
            var queue = new ConcurrentQueue<string>(inputFiles);
            var total = queue.Count;
            var completed = 0;
            var workers = new List<Task>();
            var actualWorkerCount = Math.Max(1, workerCount);

            for (var i = 0; i < actualWorkerCount; i++)
            {
                workers.Add(Task.Run(() =>
                {
                    var originalPriority = Thread.CurrentThread.Priority;
                    Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

                    try
                    {
                        string inputPath;
                        while (queue.TryDequeue(out inputPath))
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            var result = resizeService.Process(new ResizeJob(inputPath, profile, outputFolder));
                            resultReceived(result);

                            var done = Interlocked.Increment(ref completed);
                            progressChanged(done, total);
                            Thread.Sleep(1);
                        }
                    }
                    finally
                    {
                        Thread.CurrentThread.Priority = originalPriority;
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(workers);
        }
    }
}
