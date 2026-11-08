using System;
using System.IO;

// A separate, silent executable proves that the real updater restarts the installed file.
internal static class UpdateTestApplication
{
    private static void Main(string[] args)
    {
        if (args.Length != 0) return;
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "restarted.txt"), "restarted");
    }
}
