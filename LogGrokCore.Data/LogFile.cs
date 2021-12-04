using System;
using System.IO;
using System.Text;
using System.Text.Unicode;
using UtfUnknown;

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

        private readonly Encoding[] _unicodeEncodings = new[]
        {
            Encoding.Unicode, Encoding.BigEndianUnicode, Encoding.UTF8,
            Encoding.UTF32
        };
        
        private Encoding DetectEncoding()
        {
            var buffer = new byte[8192];
            var length = OpenForSequentialRead().Read(buffer, 0, buffer.Length);
            var span = buffer.AsSpan(length);

            // try to find BOM
            foreach (var unicodeEncoding in _unicodeEncodings)
            {
                if (span.StartsWith(unicodeEncoding.Preamble))
                    return unicodeEncoding;
            }
            
            // try to detect BOM-less utf-16 by crlfs 
            var crlf = "\r\n";
            var crlfUnicode = Encoding.Unicode.GetBytes(crlf);
            var crlfBigEndianUnicode = Encoding.BigEndianUnicode.GetBytes(crlf);

            var haveUnicodeCrlf = span.IndexOf(crlfUnicode) > 0;
            var haveBigEndianUnicodeCrlf = span.IndexOf(crlfBigEndianUnicode) > 0;
            if (haveUnicodeCrlf && !haveBigEndianUnicodeCrlf) 
                return Encoding.Unicode;
            if (!haveUnicodeCrlf && haveBigEndianUnicodeCrlf) 
                return Encoding.BigEndianUnicode;
            
            // try to detect BOM-less utf-16 by nulls distribution
            var evenNullCount = 0;
            var oddNullCount = 0;
            for (var i = 0; i < buffer.Length / 2; i++)
            {
                if (buffer[i*2] == 0) evenNullCount++;
                if (buffer[i*2 + 1] == 0) oddNullCount++;
            }

            var possibleUnicodeCharPercent = oddNullCount * 2 * 100 / buffer.Length;
            var possibleBigEndianCharPercent =  evenNullCount * 2 * 100/ buffer.Length;
            if (possibleUnicodeCharPercent > 70 && possibleBigEndianCharPercent < 10)
            {
                return Encoding.Unicode;
            }
            
            if (possibleBigEndianCharPercent > 70 && possibleUnicodeCharPercent < 10)
            {
                return Encoding.BigEndianUnicode;
            }

            var detectResult = CharsetDetector.DetectFromBytes(buffer);
            var detectedEncoding = detectResult.Detected?.Encoding ?? Encoding.UTF8;
            
            return detectedEncoding != Encoding.ASCII ? detectedEncoding : Encoding.UTF8;
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