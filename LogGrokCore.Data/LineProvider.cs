using LogGrokCore.Data.Virtualization;
using System;
using System.Buffers;
using System.IO;
using System.Text;

namespace LogGrokCore.Data
{
    public class LineProvider : IItemProvider<(int, string)>
    {
        private readonly Func<Stream> _streamFactory;
        private readonly ILineIndex _lineIndex;
        private readonly Encoding _encoding;

        public LineProvider(ILineIndex lineIndex, LogFile logFile)
        {
            _streamFactory = logFile.OpenForSequentialRead;
            _encoding = logFile.Encoding;
            _lineIndex = lineIndex;
        }

        public int Count => _lineIndex.Count;

        public void Fetch(int start, Span<(int, string)> values)
        {
            var (startOffset, _) = _lineIndex.GetLine(start);
            var count = values.Length;
            var (lastLineOffset, lastLineLength) = _lineIndex.GetLine(start + count - 1);

            var size = (int)(lastLineOffset + lastLineLength - startOffset);

            using var owner = MemoryPool<byte>.Shared.Rent(size);

            var span = owner.Memory.Span.Slice(0, size);

            using var stream = _streamFactory();
            stream.Seek(startOffset, SeekOrigin.Begin);
            stream.Read(span);

            using var poolOwner = MemoryPool<(long, int)>.Shared.Rent(count);
            var lineIndices = poolOwner.Memory.Span.Slice(0, count);
            _lineIndex.Fetch(start, lineIndices);
            
            for (var i = 0; i < count; i++)
            {
                var (offset, len) = lineIndices[i];
                var lineSpan = span.Slice((int)(offset - startOffset), len);
                values[i] = (start+i, _encoding.GetString(lineSpan));
            }
        }
    }
}