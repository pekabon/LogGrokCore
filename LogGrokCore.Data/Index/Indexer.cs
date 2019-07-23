using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Index
{
    public class Indexer : IDisposable
    {
        private readonly ConcurrentDictionary<IndexKey, Index> _indices =
            new ConcurrentDictionary<IndexKey, Index>(1, 16384);

        private readonly CountIndex _countIndex = new CountIndex();
        public IEnumerable<string> GetAllComponents(int componentNumber)
        {
            return _indices.Keys.Select(indexKey => new string(indexKey.GetComponent(componentNumber)));
        }

        public IItemProvider<int> GetIndexedLinesProvider(Dictionary<int, List<string>> excludedComponents)
        {
            return new IndexedLinesProvider(this, _countIndex.Counts, _countIndex.Granularity, excludedComponents);
        }

        public Index GetIndex(IndexKey key) => _indices[key];
        
        public void Add(IndexKey key, int lineNumber)
        {
            var index = _indices.GetOrAdd(key,
                indexedKey =>
                {
                    indexedKey.MakeCopy();
                    return new Index();
                });

            index.Add(lineNumber);    
            _countIndex.Add(lineNumber, _indices);            
        }

        public void Dispose()
        {
            foreach (var index in _indices.Values)
            {
                index.Dispose();
            }
            _indices.Clear();
        }

        public void Finish()
        {
            _countIndex.Finish(_indices);
        }
    }
}