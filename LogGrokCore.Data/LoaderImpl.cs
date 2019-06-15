using System;
using System.Buffers;
using System.IO;

namespace LogGrokCore.Data
{
    public class LoaderImpl
    {
        private readonly int _bufferSize;
        private readonly ILineIndex _lineIndex;

        public LoaderImpl(int bufferSize, ILineIndex lineIndex)
        {
            _bufferSize = bufferSize;
            _lineIndex = lineIndex;
        }

        public void Load(Stream stream, ReadOnlySpan<byte> cr, ReadOnlySpan<byte> lf)
        {
            var isInCrLfs = false;
            long position = 0;
            int crLength = cr.Length;
            long lineStart = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            var bufferSize = _bufferSize;
            try
            {
                _lineIndex.Add(0);
                var dataOffset = 0;

                while (true)
                {
                    var data = buffer.AsSpan(dataOffset, bufferSize);
                    var bytesRead = stream.Read(data);
                    
                    while (true)
                    {
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
                                lineStart = dataOffset + i + position;
                            }
                        }

                        position += bytesRead;

                        if (bytesRead < data.Length)
                        {
                            _lineIndex.Finish((int)(position - lineStart));
                            return;
                        }

                        if (lineStart != 0)
                        {
                            dataOffset = (int)(lineStart - position);
                            var bufferSpan = buffer.AsSpan();
                            var rest = bufferSpan.Slice(dataOffset);

                            if (bufferSize > _bufferSize && rest.Length < _bufferSize)
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                                buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                                bufferSize = _bufferSize;
                                bufferSpan = buffer.AsSpan();
                            }
                            rest.CopyTo(bufferSpan);

                            break;
                        }

                        // did not found next line start, grow buffer
                        var newBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                        buffer.CopyTo(newBuffer.AsSpan());
                        ArrayPool<byte>.Shared.Return(buffer);

                        dataOffset = bufferSize;
                        data = newBuffer.AsSpan(dataOffset, bufferSize);
                        bytesRead = stream.Read(data);
                        bufferSize *= 2;
                        buffer = newBuffer;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

    }
}