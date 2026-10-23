using System;

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
                var job = WorkerProtocol.LoadJob(args[0]);
                var imageResizer = new ImageResizeService();
                // Parallelism belongs to the process pool; threads here restore GDI+ contention.
                foreach (var inputPath in job.InputFiles)
                {
                    using (var result = imageResizer.Resize(inputPath, job.Profile, job.OutputFolder))
                    {
                        Console.WriteLine(WorkerProtocol.FormatResultLine(result));
                        Console.Out.Flush();
                    }
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}