using System;
using System.IO;
using System.Text;

namespace LogGrokCore.Data
{
    public class LogFile
    {
        private readonly  Lazy<Encoding> _encoding; 
        
        public LogFile(string filePath)
        {
            FilePath = filePath;
            FileSize = OpenFile(FilePath).Length;
            _encoding= new Lazy<Encoding>(DetectEncoding);
        }

        public string FilePath { get;  }

        public long FileSize { get; }

        public Encoding Encoding => _encoding.Value;

        public Stream OpenForSequentialRead() => OpenFile(FilePath);

        private Encoding DetectEncoding()
        {
            using var reader = new StreamReader(OpenForSequentialRead());
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
                FileOptions.SequentialScan);
        }
    }
}