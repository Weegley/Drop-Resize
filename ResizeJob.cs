using System.Collections.Generic;
using System.IO;

namespace DropResize
{
    public class ResizeJob
    {
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp",
            ".gif",
            ".tif",
            ".tiff"
        };

        public ResizeJob(string inputPath, ResizeProfile profile, string outputFolder)
        {
            InputPath = inputPath;
            Profile = profile;
            OutputFolder = outputFolder;
        }

        public string InputPath { get; private set; }
        public ResizeProfile Profile { get; private set; }
        public string OutputFolder { get; private set; }

        public static bool IsSupportedInputFile(string path)
        {
            return File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant());
        }
    }
}
