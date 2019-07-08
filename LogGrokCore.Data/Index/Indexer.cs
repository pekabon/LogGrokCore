using System.Collections.Concurrent;

namespace LogGrokCore.Data.Index
{
    
    public class Indexer
    {
        private readonly ConcurrentDictionary<IndexKey, Index> _indices =
            new ConcurrentDictionary<IndexKey, Index>(1, 16384);

        public void Add(IndexKey key, int lineNumber)
        {
            var index = _indices.GetOrAdd(key,
                indexedKey =>
                {
                    indexedKey.MakeCopy();
                    return new Index();
                });

            var justAdded = index.IsEmpty;   
            index.Add(lineNumber);    
            
            if (justAdded)
            {
                UpdateComponents(key);
            }
        }

        private void UpdateComponents(IndexKey key)
        {
        }
    }
}