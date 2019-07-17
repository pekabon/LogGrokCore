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
            FileSize = OpenFileForSequentialRead(FilePath).Length;
            _encoding= new Lazy<Encoding>(DetectEncoding);
        }

        public string FilePath { get;  }

        public long FileSize { get; }

        public Encoding Encoding => _encoding.Value;

        public Stream OpenForSequentialRead() => OpenFileForSequentialRead(FilePath);

        public Stream Open() => OpenFile(FilePath);
        
        private Encoding DetectEncoding()
        {
            using var reader = new StreamReader(OpenForSequentialRead());
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }
        
        private static Stream OpenFileForSequentialRead(string fileName)
        {
            const int bufferSize = 64 * 1024;
            return new FileStream(fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize,
                FileOptions.SequentialScan);
        }

        private static Stream OpenFile(string fileName)
        {
            return new FileStream(fileName,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
        }
    }
}