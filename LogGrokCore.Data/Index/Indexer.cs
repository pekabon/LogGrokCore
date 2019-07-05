using System.Collections.Concurrent;

namespace LogGrokCore.Data.Index
{
    public class Indexer
    {
        private ConcurrentDictionary<IndexKey, int> _indices =
            new ConcurrentDictionary<IndexKey, int>(1, 16384);

        public void Add(IndexKey key)
        {
            _indices.GetOrAdd(key, indexedKey =>
            {
                indexedKey.MakeCopy();
                return 1;
            });
        }
        
    }
}