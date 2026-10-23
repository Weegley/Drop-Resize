using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Xml.Serialization;

namespace DropResize
{
    public class ResizeResult : IDisposable
    {
        [XmlIgnore]
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

        // Only the small PNG crosses the pipe. The result owns its decoded preview.
        public byte[] ThumbnailPng
        {
            get
            {
                if (Thumbnail == null) return null;
                using (var stream = new MemoryStream())
                {
                    Thumbnail.Save(stream, ImageFormat.Png);
                    return stream.ToArray();
                }
            }
            set
            {
                Dispose();
                if (value == null || value.Length == 0) return;
                using (var stream = new MemoryStream(value, false))
                using (var image = Image.FromStream(stream)) Thumbnail = new Bitmap(image);
            }
        }

        [XmlIgnore]
        public bool Success
        {
            get { return string.IsNullOrEmpty(ErrorMessage) && !string.IsNullOrEmpty(OutputPath); }
        }

        public void Dispose()
        {
            if (Thumbnail == null) return;
            Thumbnail.Dispose();
            Thumbnail = null;
        }
    }
}
