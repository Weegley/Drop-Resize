using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace DropResize
{
    public class ImageResizeService
    {
        public ResizeResult Process(ResizeJob job)
        {
            var result = new ResizeResult
            {
                InputPath = job.InputPath,
                Status = "Processing"
            };

            try
            {
                result.OriginalFileSize = new FileInfo(job.InputPath).Length;

                using (var source = LoadImage(job.InputPath))
                {
                    result.OriginalWidth = source.Width;
                    result.OriginalHeight = source.Height;

                    var outputSize = CalculateOutputSize(source.Width, source.Height, job.Profile);
                    result.OutputWidth = outputSize.Width;
                    result.OutputHeight = outputSize.Height;

                    var outputPath = TempBatchManager.GetUniqueOutputPath(job.OutputFolder, job.InputPath, job.Profile);

                    using (var resized = new Bitmap(outputSize.Width, outputSize.Height))
                    {
                        resized.SetResolution(source.HorizontalResolution, source.VerticalResolution);

                        using (var graphics = Graphics.FromImage(resized))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            graphics.Clear(Color.Transparent);
                            graphics.DrawImage(source, 0, 0, outputSize.Width, outputSize.Height);
                        }

                        SaveImage(resized, outputPath, job.Profile);
                    }

                    result.OutputPath = outputPath;
                    result.OutputFileSize = new FileInfo(outputPath).Length;
                    result.Thumbnail = CreateThumbnail(outputPath, 96, 96);
                    result.Status = "Done";
                }
            }
            catch (Exception ex)
            {
                result.Status = "Error";
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static Image LoadImage(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        private static Size CalculateOutputSize(int width, int height, ResizeProfile profile)
        {
            var scale = Math.Min((double)profile.MaxWidth / width, (double)profile.MaxHeight / height);

            if (profile.DoNotEnlarge)
            {
                scale = Math.Min(1.0, scale);
            }

            var outputWidth = Math.Max(1, (int)Math.Round(width * scale));
            var outputHeight = Math.Max(1, (int)Math.Round(height * scale));

            return new Size(outputWidth, outputHeight);
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
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var parameters = new EncoderParameters(1))
            {
                parameters.Param[0] = new EncoderParameter(Encoder.Quality, Math.Max(1, Math.Min(100, quality)));
                image.Save(outputPath, codec, parameters);
            }
        }

        private static Image CreateThumbnail(string path, int maxWidth, int maxHeight)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var image = Image.FromStream(stream))
            {
                var scale = Math.Min((double)maxWidth / image.Width, (double)maxHeight / image.Height);
                scale = Math.Min(1.0, scale);
                var width = Math.Max(1, (int)Math.Round(image.Width * scale));
                var height = Math.Max(1, (int)Math.Round(image.Height * scale));

                var thumbnail = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(thumbnail))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.Clear(Color.Transparent);
                    graphics.DrawImage(image, 0, 0, width, height);
                }

                return thumbnail;
            }
        }
    }
}
