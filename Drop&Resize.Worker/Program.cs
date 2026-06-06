using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace DropResize
{
    internal static class WorkerProgram
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Expected job file path.");
                return 2;
            }

            try
            {
                var job = LoadJob(args[0]);
                var results = new List<WorkerResult>();
                var resultsLock = new object();
                var processor = new BatchProcessor(new ImageResizeService());

                processor.ProcessAsync(
                    job.InputFiles,
                    job.Profile,
                    job.OutputFolder,
                    job.WorkerCount,
                    CancellationToken.None,
                    result =>
                    {
                        lock (resultsLock)
                        {
                            results.Add(WorkerResult.FromResizeResult(result));
                        }

                        if (result.Thumbnail != null)
                        {
                            result.Thumbnail.Dispose();
                        }
                    },
                    (done, total) =>
                    {
                        Console.WriteLine("PROGRESS " + done + " " + total);
                        Console.Out.Flush();
                    }).GetAwaiter().GetResult();

                SaveResults(job.ResultPath, results);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static WorkerJob LoadJob(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(WorkerJob));
                return (WorkerJob)serializer.Deserialize(stream);
            }
        }

        private static void SaveResults(string path, List<WorkerResult> results)
        {
            using (var stream = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(List<WorkerResult>));
                serializer.Serialize(stream, results);
            }
        }
    }
}
