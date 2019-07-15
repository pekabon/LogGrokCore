using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public sealed class ParsedBufferConsumer
    {
        private readonly BlockingCollection<(long startOffset, int lineCount, string buffer)> _queue
            = new BlockingCollection<(long, int, string)>(4);

        private readonly LineIndex _lineIndex;
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _logMetaInformation;
        private readonly StringPool _stringPool;
        private readonly long _fileSize;

        public ParsedBufferConsumer(
            LineIndex lineIndex,
            Indexer indexer,
            LogMetaInformation logMetaInformation,
            LogFile logFile,
            StringPool stringPool)
        {
            _lineIndex = lineIndex;
            _indexer = indexer;
            _logMetaInformation = logMetaInformation;
            _stringPool = stringPool;
            _fileSize = logFile.FileSize;
            Task.Factory.StartNew(ConsumeBuffers);
        }

        public void AddParsedBuffer(long bufferStartOffset, int lineCount, string parsedBuffer)
        {
            _queue.Add((bufferStartOffset, lineCount, parsedBuffer));
        }

        public void CompleteAdding()
        {
            _queue.CompleteAdding();
        }

        private unsafe void ConsumeBuffers()
        {
            long lineOffsetFromBufferStart = 0;

            var componentsCount = _logMetaInformation.IndexedFieldNumbers.Length;
            foreach (var (bufferStartOffset, lineCount, buffer) in _queue.GetConsumingEnumerable())
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

            _lineIndex.Finish((int) (_fileSize - lineOffsetFromBufferStart));
        }
    }
}