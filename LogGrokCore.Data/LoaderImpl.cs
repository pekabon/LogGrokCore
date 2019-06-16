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
            int crLength = cr.Length;

            var bufferStartPosition = 0l;
            var lineStartFromCurrentDataOffset = 0;

            var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
            var bufferSize = _bufferSize;
            try
            {
                var dataOffsetFromBufferStart = 0;
                long streamPosition = 0;
                while (true)
                {
                    bufferStartPosition = streamPosition - dataOffsetFromBufferStart;
                    var data = buffer.AsSpan(dataOffsetFromBufferStart,
                        bufferSize - dataOffsetFromBufferStart);
                    var bytesRead = stream.Read(data);
                    streamPosition += bytesRead;

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
                                _lineIndex.Add(
                                    bufferStartPosition
                                    + dataOffsetFromBufferStart
                                    + lineStartFromCurrentDataOffset);

                                lineStartFromCurrentDataOffset = i;
                            }
                        }

                        var lineOffsetFromBufferStart =
                            dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                        if (bytesRead < data.Length)
                        {
                            var bufferEndOffset = bytesRead + dataOffsetFromBufferStart;

                            _lineIndex.Add(
                                bufferStartPosition
                                + dataOffsetFromBufferStart
                                + lineStartFromCurrentDataOffset);
                            _lineIndex.Finish(
                                bufferEndOffset - lineOffsetFromBufferStart);

                            return;
                        }

                        if (lineOffsetFromBufferStart > 0)
                        {
                            // found line(s) inside the buffer
                            // copy tail of buffer to new one
                            dataOffsetFromBufferStart = bufferSize - lineOffsetFromBufferStart;
                            lineStartFromCurrentDataOffset = - dataOffsetFromBufferStart;
                            bufferStartPosition = streamPosition + lineOffsetFromBufferStart;

                            var bufferSpan = buffer.AsSpan();
                            var rest = bufferSpan.Slice(lineOffsetFromBufferStart);

                            if (bufferSize <= _bufferSize || rest.Length >= _bufferSize)
                            {
                                rest.CopyTo(bufferSpan);
                            }
                            else
                            {
                                var oldBuffer = buffer;
                                buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                                bufferSize = _bufferSize;
                                bufferSpan = buffer.AsSpan();
                                rest.CopyTo(bufferSpan);
                                ArrayPool<byte>.Shared.Return(oldBuffer);
                            }
                            break;
                        }

                        // did not found next line start, grow buffer
                        var newBuffer = ArrayPool<byte>.Shared.Rent(bufferSize * 2);
                        buffer.CopyTo(newBuffer.AsSpan());
                        ArrayPool<byte>.Shared.Return(buffer);

                        var lineOffsetFromBufferStart_ =
                            dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                        dataOffsetFromBufferStart = bufferSize;

                        lineStartFromCurrentDataOffset =
                            lineOffsetFromBufferStart_ - dataOffsetFromBufferStart;

                        data = newBuffer.AsSpan(dataOffsetFromBufferStart, bufferSize);
                        bytesRead = stream.Read(data);
                        streamPosition += bytesRead;

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