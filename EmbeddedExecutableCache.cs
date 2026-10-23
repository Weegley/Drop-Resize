using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace DropResize
{
    public static class EmbeddedExecutableCache
    {
        public static string GetExecutablePath(string executableName, string cacheRoot = null)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(executableName))
            {
                if (resource == null)
                {
                    var externalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, executableName);
                    if (File.Exists(externalPath)) return externalPath;
                    throw new FileNotFoundException("Embedded executable was not found: " + executableName);
                }

                string contentHash;
                using (var sha256 = SHA256.Create()) contentHash = BitConverter.ToString(sha256.ComputeHash(resource)).Replace("-", "");
                var cacheFolder = Path.Combine(cacheRoot ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Drop&Resize", "Tools"), contentHash);
                Directory.CreateDirectory(cacheFolder);
                var executablePath = Path.Combine(cacheFolder, executableName);
                if (File.Exists(executablePath)) return executablePath;

                // A content-addressed cache reuses running tools; atomic rename also permits concurrent app instances.
                var temporaryPath = executablePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    resource.Position = 0;
                    using (var output = File.Create(temporaryPath)) resource.CopyTo(output);
                    try { File.Move(temporaryPath, executablePath); }
                    catch (IOException) { if (!File.Exists(executablePath)) throw; }
                }
                finally { if (File.Exists(temporaryPath)) File.Delete(temporaryPath); }
                return executablePath;
            }
        }
    }
}
