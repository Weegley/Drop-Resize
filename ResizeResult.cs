using System.Drawing;

namespace DropResize
{
    public class ResizeResult
    {
        public Image Thumbnail { get; set; }
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

        public bool Success
        {
            get { return string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(OutputPath); }
        }
    }
}
