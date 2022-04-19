using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LogGrokCore.Data
{
    public class LoaderImpl
    {
        private readonly int _bufferSize;
        private readonly ILineDataConsumer _lineDataConsumer;

        public LoaderImpl(int bufferSize, ILineDataConsumer lineDataConsumer)
        {
            _bufferSize = bufferSize;
            _lineDataConsumer = lineDataConsumer;
        }

        public async Task<long> Load(Stream stream, byte[] cr, byte[] lf, CancellationToken cancellationToken)
        {
            var isInCrLfs = false;
            var crLength = cr.Length;

            var firstBytes = new[] { cr[0], lf[0] };
            var isIsSingleByteCrLf = cr.Length == 1 && lf.Length == 1; 
            
            var lineStartFromCurrentDataOffset = 0;

            var bufferOwner = MemoryPool<byte>.Shared.Rent(_bufferSize);

            var bufferSize = _bufferSize;
            var linesList = new PooledList<(long offset, int start, int length)>();

            var dataOffsetFromBufferStart = 0;
            long streamPosition = stream.Position;
            while (!cancellationToken.IsCancellationRequested)
            {
                var bufferStartPosition = streamPosition - dataOffsetFromBufferStart;
                var dataLength = bufferSize - dataOffsetFromBufferStart;
                var memory = bufferOwner.Memory;
                var bytesRead = stream.Read(memory.Span.Slice(dataOffsetFromBufferStart, dataLength));
                streamPosition += bytesRead;

                while (true)
                {
                    var i = 0;
                    while (i < bytesRead)
                    {
                        if (!isInCrLfs)
                        {
                            var crlfPosition = memory.Span[(dataOffsetFromBufferStart + i)..].IndexOfAny(firstBytes);
                            if (crlfPosition < 0)
                            {
                                break;
                            }

                            i += crlfPosition;
                        }

                        if (i >= bytesRead) break;

                        
                        if ((!isInCrLfs && isIsSingleByteCrLf) || 
                            memory.Span.Slice((dataOffsetFromBufferStart + i), crLength).SequenceEqual(cr) ||
                            memory.Span.Slice((dataOffsetFromBufferStart + i), crLength).SequenceEqual(lf))
                        {
                            isInCrLfs = true;
                        }
                        else if (isInCrLfs)
                        {
                            isInCrLfs = false;

                            var lineStartInBuffer =
                                dataOffsetFromBufferStart
                                + lineStartFromCurrentDataOffset;

                            linesList.Add((bufferStartPosition + lineStartInBuffer,
                                lineStartInBuffer,
                                i + dataOffsetFromBufferStart - lineStartInBuffer));
                            lineStartFromCurrentDataOffset = i;
                        }

                        i += crLength;
                    }

                    var lineOffsetFromBufferStart =
                        dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                    if (bytesRead < dataLength)
                    {
                        var bufferEndOffset = bytesRead + dataOffsetFromBufferStart;
                        linesList.Add((bufferStartPosition + lineOffsetFromBufferStart,
                            lineOffsetFromBufferStart,
                            bufferEndOffset - lineOffsetFromBufferStart));
                        await _lineDataConsumer.AddLineData(bufferOwner, linesList);
                        await _lineDataConsumer.CompleteAdding(stream.Position);
                        return stream.Position;
                    }

                    if (lineOffsetFromBufferStart > 0)
                    {
                        // found line(s) inside the buffer
                        // copy tail of buffer to new one
                        dataOffsetFromBufferStart = bufferSize - lineOffsetFromBufferStart;
                        lineStartFromCurrentDataOffset = -dataOffsetFromBufferStart;

                        var oldBufferOwner = bufferOwner;

                        var restLength = memory.Length - lineOffsetFromBufferStart;
                        bufferSize = Math.Max(_bufferSize, restLength);
                        bufferOwner = MemoryPool<byte>.Shared.Rent(bufferSize);
                        memory.Span[lineOffsetFromBufferStart..].CopyTo(bufferOwner.Memory.Span);
                        
                        await _lineDataConsumer.AddLineData(oldBufferOwner, linesList);
                        linesList = new PooledList<(long offset, int start, int length)>();
                        break;
                    }

                    // did not found next line start, grow buffer
                    var newBufferOwner = MemoryPool<byte>.Shared.Rent(bufferSize * 2);
                    memory.CopyTo(newBufferOwner.Memory);
                    bufferOwner.Dispose();

                    bufferOwner = newBufferOwner;
                    memory = newBufferOwner.Memory;

                    var lineOffsetFromBufferStart_ =
                        dataOffsetFromBufferStart + lineStartFromCurrentDataOffset;

                    dataOffsetFromBufferStart = bufferSize;

                    lineStartFromCurrentDataOffset =
                        lineOffsetFromBufferStart_ - dataOffsetFromBufferStart;

                    bytesRead = stream.Read(memory.Span.Slice(dataOffsetFromBufferStart, bufferSize));
                    streamPosition += bytesRead;

                    bufferSize *= 2;
                }
            }

            return 0;
        }
    }
}