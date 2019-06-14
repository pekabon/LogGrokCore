using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class Loader
    {
        private readonly LineIndex _lineIndex;
        private readonly Task _loadingTask;
        private const int BufferSize = 1024*1024;

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

            var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
            var bufferSize = BufferSize;
            try
            {
                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, bufferSize);
                    var data = buffer.AsSpan();

                    for (int i = 0; i < bytesRead; i += crLength)
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

                    if (bytesRead < bufferSize)
                    {
                        _lineIndex.Finish((int)(bytesRead - lineStart));
                        break;
                    }

                    if (lineStart != 0)
                    {
                        position += lineStart;
                        stream.Position = position;
                        lineStart = 0;
                        if (bufferSize > BufferSize)
                        {
                            ArrayPool<byte>.Shared.Return(buffer);
                            buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
                            bufferSize = BufferSize;
                        }
                    }

                    // did not found next line start, grow buffer
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                    bufferSize *= 2;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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