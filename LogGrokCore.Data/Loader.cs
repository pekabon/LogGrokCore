using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class Loader
    {
        public Loader(Func<Stream> streamFactory)
        {
            var encoding = DetectEncoding(streamFactory());
            _loadingTask = Task.Factory.StartNew(() => Load(streamFactory(), encoding.GetBytes("\r"), encoding.GetBytes("\n")));
        }

        public LineIndex LineIndex { get; } = new LineIndex();

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
                        LineIndex.Add(lineStart);
                        lineStart = i + position;
                    }
                }
                position += bytesRead;

                if (bytesRead < _buffer.Length)
                {
                    LineIndex.Finish((int)(position - lineStart));
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

        private byte[] _buffer = new byte[4 * 1024 * 1024];
        private Task _loadingTask;
    }

}