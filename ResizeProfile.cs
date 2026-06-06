using System.Collections.Generic;

namespace DropResize
{
    public enum ResizeMode
    {
        Fit
    }

    public enum OutputFormat
    {
        Jpeg,
        Png,
        Bmp
    }

    public class ResizeProfile
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public ResizeMode ResizeMode { get; set; }
        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }
        public bool DoNotEnlarge { get; set; }
        public OutputFormat OutputFormat { get; set; }
        public long JpegQuality { get; set; }
        public string Suffix { get; set; }
        public bool IsBuiltIn { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public static List<ResizeProfile> CreateBuiltInProfiles()
        {
            return new List<ResizeProfile>
            {
                new ResizeProfile
                {
                    Id = "email-1280-jpg-85",
                    Name = "Email 1280 JPG 85",
                    ResizeMode = ResizeMode.Fit,
                    MaxWidth = 1280,
                    MaxHeight = 1280,
                    DoNotEnlarge = true,
                    OutputFormat = OutputFormat.Jpeg,
                    JpegQuality = 85,
                    Suffix = "_email",
                    IsBuiltIn = true
                },
                new ResizeProfile
                {
                    Id = "web-1920-jpg-85",
                    Name = "Web 1920 JPG 85",
                    ResizeMode = ResizeMode.Fit,
                    MaxWidth = 1920,
                    MaxHeight = 1920,
                    DoNotEnlarge = true,
                    OutputFormat = OutputFormat.Jpeg,
                    JpegQuality = 85,
                    Suffix = "_web",
                    IsBuiltIn = true
                },
                new ResizeProfile
                {
                    Id = "small-800-jpg-80",
                    Name = "Small 800 JPG 80",
                    ResizeMode = ResizeMode.Fit,
                    MaxWidth = 800,
                    MaxHeight = 800,
                    DoNotEnlarge = true,
                    OutputFormat = OutputFormat.Jpeg,
                    JpegQuality = 80,
                    Suffix = "_small",
                    IsBuiltIn = true
                },
                new ResizeProfile
                {
                    Id = "png-1280",
                    Name = "PNG 1280",
                    ResizeMode = ResizeMode.Fit,
                    MaxWidth = 1280,
                    MaxHeight = 1280,
                    DoNotEnlarge = true,
                    OutputFormat = OutputFormat.Png,
                    JpegQuality = 90,
                    Suffix = "_resized",
                    IsBuiltIn = true
                }
            };
        }

        public ResizeProfile Clone()
        {
            return new ResizeProfile
            {
                Id = Id,
                Name = Name,
                ResizeMode = ResizeMode,
                MaxWidth = MaxWidth,
                MaxHeight = MaxHeight,
                DoNotEnlarge = DoNotEnlarge,
                OutputFormat = OutputFormat,
                JpegQuality = JpegQuality,
                Suffix = Suffix,
                IsBuiltIn = IsBuiltIn
            };
        }
    }
}
