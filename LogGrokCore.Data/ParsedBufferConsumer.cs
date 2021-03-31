using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public sealed class ParsedBufferConsumer
    {
        private readonly BlockingCollection<(long startOffset, int lineCount, string buffer)> _queue
            = new(4);

        private readonly LineIndex _lineIndex;
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _logMetaInformation;
        private readonly StringPool _stringPool;
        private long? _totalBytesRead;

        public ParsedBufferConsumer(
            LineIndex lineIndex,
            Indexer indexer,
            LogMetaInformation logMetaInformation,
            StringPool stringPool)
        {
            _lineIndex = lineIndex;
            _indexer = indexer;
            _logMetaInformation = logMetaInformation;
            _stringPool = stringPool;
            Task.Factory.StartNew(ConsumeBuffers);
        }

        public void AddParsedBuffer(long bufferStartOffset, int lineCount, string parsedBuffer)
        {
            _queue.Add((bufferStartOffset, lineCount, parsedBuffer));
        }

        public void CompleteAdding(long totalBytesRead)
        {
            _totalBytesRead = totalBytesRead;
            _queue.CompleteAdding();
        }

        private unsafe void ConsumeBuffers()
        {
            long lineOffsetFromBufferStart = 0;

            var componentsCount = _logMetaInformation.IndexedFieldNumbers.Length;
            
#pragma warning disable CS8619
            foreach (var (bufferStartOffset, lineCount, buffer) in _queue.GetConsumingEnumerable())
#pragma warning restore CS8619
            {
                var metaOffset = 0;
                fixed (char* start = buffer)
                {
                    for (int idx = 0; idx < lineCount; idx++)
                    {
                        var lineMetaInformation = LineMetaInformation.Get(start + metaOffset, componentsCount);
                        lineOffsetFromBufferStart = bufferStartOffset + 
                                                        lineMetaInformation.LineOffsetFromBufferStart;
                        var lineNum = _lineIndex.Add(lineOffsetFromBufferStart);

                        var indexKey = new IndexKey(buffer, metaOffset, componentsCount);
                        _indexer.Add(indexKey, lineNum);
                        metaOffset += lineMetaInformation.TotalSizeWithPayloadCharsAligned;
                    }
                }
                _stringPool.Return(buffer);
            }

            if (_totalBytesRead is not { } fileSize)
            {
                throw new InvalidOperationException();
            }
            _lineIndex.Finish((int) (fileSize - lineOffsetFromBufferStart));
            _indexer.Finish();
        }
    }
}