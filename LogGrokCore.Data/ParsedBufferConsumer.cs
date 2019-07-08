using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public sealed class ParsedBufferConsumer
    {
        private readonly BlockingCollection<string> _queue
            = new BlockingCollection<string>(4);

        private readonly StringPool _stringPool;
        private readonly Indexer _indexer;
        private readonly LogMetaInformation _logMetaInformation;

        public ParsedBufferConsumer(Indexer indexer, LogMetaInformation logMetaInformation, StringPool stringPool)
        {
            _indexer = indexer;
            _logMetaInformation = logMetaInformation;
            _stringPool = stringPool;
            Task.Factory.StartNew(ConsumeBuffers);
        }

        public void AddParsedBuffer(string parsedBuffer)
        {
            _queue.Add(parsedBuffer);
        }

        public void CompleteAdding()
        {
            _queue.CompleteAdding();
        }

        private unsafe void ConsumeBuffers()
        {
            var lineCounter = 0;
            foreach (var buffer in _queue.GetConsumingEnumerable())
            {
                var componentsCount = _logMetaInformation.IndexedFieldNumbers.Length;
                var bufferOffset = 0;
                fixed (char* start = buffer)
                {
                    while(true)
                    {
                        var node = new LineMetaInformationNode(new Span<int>(start + bufferOffset,
                            buffer.Length * sizeof(int) / sizeof(char)), componentsCount);

                        var indexKey = new IndexKey(buffer, bufferOffset, componentsCount);
                        _indexer.Add(indexKey, lineCounter++);
                     
                        var  nextOffset = node.NextNodeOffset;
                        if (nextOffset < 0)
                            break;
                        bufferOffset = nextOffset;
                    }
                }
                _stringPool.Return(buffer);
            }            
        }
    }
}