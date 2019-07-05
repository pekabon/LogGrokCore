using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogGrokCore.Data.Index;
using LogGrokCore.Data.Monikers;

namespace LogGrokCore.Data
{
    public class ParsedBufferConsumer
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
        
        private unsafe void ConsumeBuffers()
        {
            foreach (var buffer in _queue.GetConsumingEnumerable())
            {
                var componentsCount = _logMetaInformation.IndexedFieldNumbers.Length;
                var bufferOffset = 0;
                fixed (char* start = buffer)
                {
                    var node = new LineMetaInformationNode(new Span<int>(start,
                        buffer.Length * sizeof(int) / sizeof(char)), componentsCount);
                    
                    while(true)
                    {
                        var indexKey = new IndexKey(buffer, bufferOffset, componentsCount);
                        var s = indexKey.ToString();
                        _indexer.Add(indexKey);
                     
                        var nextOffset = node.NextNodeOffset;
                        if (nextOffset < 0)
                            break;
                        bufferOffset += nextOffset;

                    }
                }
                _stringPool.Return(buffer);
            }            
        }
    }
}