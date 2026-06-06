using System.Collections.Generic;

namespace DropResize
{
    public class WorkerJob
    {
        public List<string> InputFiles { get; set; }
        public ResizeProfile Profile { get; set; }
        public string OutputFolder { get; set; }
        public int WorkerCount { get; set; }
        public string ResultPath { get; set; }
    }

    public class WorkerResult
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public int OriginalWidth { get; set; }
        public int OriginalHeight { get; set; }
        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }
        public long OriginalFileSize { get; set; }
        public long OutputFileSize { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }

        public static WorkerResult FromResizeResult(ResizeResult result)
        {
            return new WorkerResult
            {
                InputPath = result.InputPath,
                OutputPath = result.OutputPath,
                OriginalWidth = result.OriginalWidth,
                OriginalHeight = result.OriginalHeight,
                OutputWidth = result.OutputWidth,
                OutputHeight = result.OutputHeight,
                OriginalFileSize = result.OriginalFileSize,
                OutputFileSize = result.OutputFileSize,
                Status = result.Status,
                ErrorMessage = result.ErrorMessage
            };
        }

        public ResizeResult ToResizeResult()
        {
            return new ResizeResult
            {
                InputPath = InputPath,
                OutputPath = OutputPath,
                OriginalWidth = OriginalWidth,
                OriginalHeight = OriginalHeight,
                OutputWidth = OutputWidth,
                OutputHeight = OutputHeight,
                OriginalFileSize = OriginalFileSize,
                OutputFileSize = OutputFileSize,
                Status = Status,
                ErrorMessage = ErrorMessage
            };
        }
    }
}
