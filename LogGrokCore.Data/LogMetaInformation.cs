using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        private readonly Lazy<Encoding> _encoding;
        public string[] FieldNames { get; }

        public LogMetaInformation(string fileName, Regex lineRegex, int componentCount)
        {
            FileName = fileName;
            LineRegex = lineRegex;
            ComponentCount = componentCount;
            FieldNames = new[] {"Time", "Thread", "Text"};
            StreamFactory = () => OpenFile(fileName);
            _encoding = new Lazy<Encoding>(() => DetectEncoding(StreamFactory()));
        }

        public string FileName { get;  }

        public Regex LineRegex { get; }

        public int ComponentCount { get; }

        public Encoding Encoding => _encoding.Value;
        
        public Func<Stream> StreamFactory { get; }

        private Encoding DetectEncoding(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }

        private static Stream OpenFile(string fileName)
        {
            const int bufferSize = 64 * 1024;
            return new FileStream(fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                options: FileOptions.SequentialScan);
        }

    }
}