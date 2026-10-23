using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DropResize
{
    public class ImageResizeService
    {
        private static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tif", ".tiff" };
        private static readonly Lazy<ImageCodecInfo> JpegCodec = new Lazy<ImageCodecInfo>(
            () => ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid));

        public static bool IsSupportedInputFile(string path)
        {
            return File.Exists(path) && SupportedExtensions.Contains(Path.GetExtension(path));
        }

        public ResizeResult Resize(string inputPath, ResizeProfile profile, string outputFolder)
        {
            var result = new ResizeResult
            {
                InputPath = inputPath,
                Status = "Processing"
            };
            string reservedOutputPath = null;

            try
            {
                result.OriginalFileSize = new FileInfo(inputPath).Length;

                // GDI+ can decode lazily. Keep the stream alive until drawing is finished.
                using (var stream = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sourceImage = Image.FromStream(stream, false, false))
                {
                    result.OriginalWidth = sourceImage.Width;
                    result.OriginalHeight = sourceImage.Height;

                    var outputSize = profile.CalculateOutputSize(sourceImage.Width, sourceImage.Height);
                    result.OutputWidth = outputSize.Width;
                    result.OutputHeight = outputSize.Height;

                    var outputPath = TempBatchManager.ReserveOutputPath(outputFolder, inputPath, profile);
                    reservedOutputPath = outputPath;

                    using (var resizedImage = CreateResizedBitmap(sourceImage, outputSize))
                    {
                        SaveImage(resizedImage, outputPath, profile);
                        result.Thumbnail = CreateThumbnail(resizedImage, 96, 96);
                    }

                    result.OutputPath = outputPath;
                    result.OutputFileSize = new FileInfo(outputPath).Length;
                    result.Status = "Done";
                }
            }
            catch (Exception ex)
            {
                result.Dispose();
                if (reservedOutputPath != null)
                {
                    try { File.Delete(reservedOutputPath); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
                result.OutputPath = null;
                result.Status = "Error";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static void SaveImage(Bitmap image, string outputPath, ResizeProfile profile)
        {
            switch (profile.OutputFormat)
            {
                case OutputFormat.Png:
                    image.Save(outputPath, ImageFormat.Png);
                    break;
                case OutputFormat.Bmp:
                    image.Save(outputPath, ImageFormat.Bmp);
                    break;
                default:
                    SaveJpeg(image, outputPath, profile.JpegQuality);
                    break;
            }
        }

        private static void SaveJpeg(Bitmap image, string outputPath, long quality)
        {
            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, Math.Max(1, Math.Min(100, quality)));
                image.Save(outputPath, JpegCodec.Value, parameters);
            }
        }

        public static Image CreateThumbnail(string path, int maxWidth, int maxHeight)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var image = Image.FromStream(stream))
            {
                return CreateThumbnail(image, maxWidth, maxHeight);
            }
        }

        private static Image CreateThumbnail(Image image, int maxWidth, int maxHeight)
        {
            var scale = Math.Min(1.0, Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height));
            var width = Math.Max(1, (int)Math.Round(image.Width * scale));
            var height = Math.Max(1, (int)Math.Round(image.Height * scale));
            return CreateResizedBitmap(image, new Size(width, height));
        }

        private static Bitmap CreateResizedBitmap(Image sourceImage, Size targetSize)
        {
            var resizedImage = new Bitmap(targetSize.Width, targetSize.Height);
            try
            {
                var horizontalDpi = sourceImage.HorizontalResolution;
                var verticalDpi = sourceImage.VerticalResolution;
                if (horizontalDpi > 0 && verticalDpi > 0 && !float.IsInfinity(horizontalDpi) && !float.IsInfinity(verticalDpi))
                    resizedImage.SetResolution(horizontalDpi, verticalDpi);
                using (var graphics = Graphics.FromImage(resizedImage))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.Clear(Color.Transparent);
                    // Pixel bounds keep image geometry independent of source DPI.
                    graphics.DrawImage(sourceImage, new Rectangle(Point.Empty, targetSize),
                        0, 0, sourceImage.Width, sourceImage.Height, GraphicsUnit.Pixel);
                }
                return resizedImage;
            }
            catch
            {
                resizedImage.Dispose();
                throw;
            }
        }
    }
}
