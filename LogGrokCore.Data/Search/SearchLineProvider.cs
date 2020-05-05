using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Search
{
    public class SearchLineProvider : IItemProvider<(int originalIndex, string line)>
    {
        private readonly SearchLineIndex _lineIndex;
        private readonly Func<Stream> _streamFactory;
        private readonly Encoding _encoding;

        public SearchLineProvider(SearchLineIndex searchLineIndex, LogFile logFile)
        {
            _lineIndex = searchLineIndex;
            _streamFactory = logFile.OpenForSequentialRead;
            _encoding = logFile.Encoding;
        }

        public int Count => _lineIndex.Count;
        
        public void Fetch(int start, Span<(int originalIndex, string line)> values)
        {
            var count = _lineIndex.Count;
            var length = values.Length;
            var total = start + length;
            Debug.Assert(count >= total);
            
            using var stream = _streamFactory();
                
            for (var index = start; index < values.Length + start; index++)
            {
                var (originalIndex, offset, len) = _lineIndex.GetLine(index);
                stream.Seek(offset, SeekOrigin.Begin);
                using var owner = MemoryPool<byte>.Shared.Rent(len);
                var span = owner.Memory.Span.Slice(0, len);
                stream.Read(span);
                values[index - start] = (originalIndex, _encoding.GetString(span).TrimEnd());
            }
            //TODO: make optimizations for continuous blocks
        }
    }
}