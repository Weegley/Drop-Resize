using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace DropResize
{
    public class WorkerJob
    {
        public List<string> InputFiles { get; set; }
        public ResizeProfile Profile { get; set; }
        public string OutputFolder { get; set; }
    }

    public static class WorkerProtocol
    {
        public const string ResultPrefix = "RESULT ";
        private static readonly XmlSerializer JobSerializer = new XmlSerializer(typeof(WorkerJob));
        private static readonly XmlSerializer ResultSerializer = new XmlSerializer(typeof(ResizeResult));

        public static void SaveJob(string path, WorkerJob job)
        {
            using (var stream = File.Create(path)) JobSerializer.Serialize(stream, job);
        }

        public static WorkerJob LoadJob(string path)
        {
            using (var stream = File.OpenRead(path)) return (WorkerJob)JobSerializer.Deserialize(stream);
        }

        public static string FormatResultLine(ResizeResult result)
        {
            using (var stream = new MemoryStream())
            {
                // Base64 keeps file names, errors and XML line breaks in a single stdout record.
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { NewLineHandling = NewLineHandling.Entitize }))
                    ResultSerializer.Serialize(writer, result);
                return ResultPrefix + Convert.ToBase64String(stream.ToArray());
            }
        }

        public static ResizeResult ReadResultLine(string line)
        {
            if (!line.StartsWith(ResultPrefix, StringComparison.Ordinal))
                throw new InvalidDataException("Unexpected worker response.");
            using (var stream = new MemoryStream(Convert.FromBase64String(line.Substring(ResultPrefix.Length))))
                return (ResizeResult)ResultSerializer.Deserialize(stream);
        }
    }
}