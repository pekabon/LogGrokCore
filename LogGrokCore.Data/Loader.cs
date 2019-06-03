using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class Loader
    {
        private readonly LineIndex _lineIndex;
        private readonly byte[] _buffer = new byte[1024 * 1024];
        private readonly Task _loadingTask;

        public Loader(Func<Stream> streamFactory)
        {
            var encoding = DetectEncoding(streamFactory());
            _loadingTask = Task.Factory.StartNew(() => Load(streamFactory(), encoding.GetBytes("\r"), encoding.GetBytes("\n")));
            _lineIndex = new LineIndex();
            LineProvider = new LineProvider(_lineIndex, streamFactory, encoding);
        }

        public LineProvider LineProvider { get; }

        public bool IsLoading => !_loadingTask.IsCompleted;

        private void Load(Stream stream, ReadOnlySpan<byte> cr, ReadOnlySpan<byte> lf) 
        {
            var isInCrLfs = false;
            var lineStart = 0L;
            long position = 0;
            int crLength = cr.Length;

            while (true)
            {
                var bytesRead = stream.Read(_buffer, 0, _buffer.Length);
                var data = _buffer.AsSpan();
                
                for (int i = 0; i < data.Length; i+= crLength)
                {
                    var current = data.Slice(i, crLength);
                    if (current.SequenceEqual(cr) || current.SequenceEqual(lf))
                    {
                        isInCrLfs = true;
                    }
                    else if (isInCrLfs)
                    {
                        isInCrLfs = false;
                        _lineIndex.Add(lineStart);
                        lineStart = i + position;
                    }
                }
                position += bytesRead;

                if (bytesRead < _buffer.Length)
                {
                    _lineIndex.Finish((int)(position - lineStart));
                    break;
                }
            }
        }

        private Encoding DetectEncoding(Stream stream)
        {
            using var reader = new StreamReader(stream);
            _ = reader.ReadLine();
            return reader.CurrentEncoding;
        }
    }
}