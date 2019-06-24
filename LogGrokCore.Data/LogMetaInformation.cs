using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class LogMetaInformation
    {
        private readonly Lazy<Encoding> _encoding;

        public LogMetaInformation(string fileName, Func<Stream> streamFactory, Regex lineRegex, int componentCount)
        {
            FileName = fileName;
            LineRegex = lineRegex;
            ComponentCount = componentCount;
            
            _encoding = new Lazy<Encoding>(() => DetectEncoding(streamFactory()));
        }

        public string FileName { get;  }
        public Regex LineRegex { get; }

        public int ComponentCount { get; }

        public Encoding Encoding => _encoding.Value;
        
        private Encoding DetectEncoding(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }
    }
}