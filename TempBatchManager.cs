using System;
using System.IO;

namespace DropResize
{
    public class TempBatchManager
    {
        private TempBatchManager(string batchFolder)
        {
            BatchFolder = batchFolder;
        }

        public string BatchFolder { get; private set; }

        public void Delete()
        {
            if (string.IsNullOrEmpty(BatchFolder) || !Directory.Exists(BatchFolder))
            {
                return;
            }

            try
            {
                Directory.Delete(BatchFolder, true);
            }
            catch
            {
                // Temporary output cleanup should not prevent the app from closing.
            }
        }

        public static void DeleteRoot()
        {
            var root = Path.Combine(Path.GetTempPath(), "Drop&Resize");
            if (!Directory.Exists(root))
            {
                return;
            }

            try
            {
                Directory.Delete(root, true);
            }
            catch
            {
                // Leftover temporary files are non-fatal and may be locked by drag targets.
            }
        }

        public static TempBatchManager Create()
        {
            var root = Path.Combine(Path.GetTempPath(), "Drop&Resize");
            Directory.CreateDirectory(root);

            var folderName =
                "batch_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") +
                "_" +
                Guid.NewGuid().ToString("N").Substring(0, 8);

            var folder = Path.Combine(root, folderName);
            Directory.CreateDirectory(folder);

            return new TempBatchManager(folder);
        }

        public static string GetUniqueOutputPath(string outputFolder, string inputPath, ResizeProfile profile)
        {
            var baseName = MakeSafeFileName(Path.GetFileNameWithoutExtension(inputPath));
            var extension = GetExtension(profile.OutputFormat);
            var candidate = Path.Combine(outputFolder, baseName + profile.Suffix + extension);
            var index = 2;

            while (File.Exists(candidate))
            {
                candidate = Path.Combine(outputFolder, baseName + profile.Suffix + "_" + index + extension);
                index++;
            }

            return candidate;
        }

        private static string MakeSafeFileName(string name)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(invalid, '_');
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                return "image";
            }

            return name;
        }

        private static string GetExtension(OutputFormat format)
        {
            switch (format)
            {
                case OutputFormat.Png:
                    return ".png";
                case OutputFormat.Bmp:
                    return ".bmp";
                default:
                    return ".jpg";
            }
        }
    }
}
