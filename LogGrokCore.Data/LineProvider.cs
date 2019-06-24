using LogGrokCore.Data.Virtualization;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogGrokCore.Data
{
    public class LineSet : IDisposable
    {
        public LineSet(ILineParser parser, int sourceByteCount, Encoding encoding)
        {
            var maxCount = encoding.GetMaxCharCount(sourceByteCount);
            _bufferString = StringPool.Rent(maxCount + maxCount / 4);
            _parser = parser;
        }

        public void Add(Span<byte> lineData)
        {
            
        }

        private static readonly StringPool StringPool = new StringPool();
        private string _bufferString;
        
        private ILineParser _parser;

        public void Dispose()
        {
            StringPool.Return(_bufferString);
        }
    }

    public class LineProvider : IItemProvider<string>
    {
        private readonly Func<Stream> _streamFactory;
        private readonly LineIndex _lineIndex;
        private readonly Encoding _encoding;

        public LineProvider(LineIndex lineIndex, Func<Stream> streamFactory, Encoding encoding)
        {
            _streamFactory = streamFactory;
            _lineIndex = lineIndex;
            _encoding = encoding;
        }

        public int Count => _lineIndex.Count;

        public IList<string> Fetch(int start, int count)
        {
            var (startOffset, _) = _lineIndex.GetLine(start);
            var (lastLineOffset, lastLineLength) = _lineIndex.GetLine(start + count - 1);

            var size = (int)(lastLineOffset + lastLineLength - startOffset);

            using var owner = MemoryPool<byte>.Shared.Rent(size);

            var span = owner.Memory.Span.Slice(0, size);

            using var stream = _streamFactory();
            stream.Seek(startOffset, SeekOrigin.Begin);
            stream.Read(span);
            
            var result = new List<string>(count);
            for (var i = start; i < start + count; i++)
            {
                var (offset, len) = _lineIndex.GetLine(i);
                var lineSpan = span.Slice((int)(offset - startOffset), len);
                result.Add(_encoding.GetString(lineSpan).TrimEnd());
            }
            return result;
        }
    }
}