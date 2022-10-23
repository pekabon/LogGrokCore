using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Index;

public class SubIndexer : IndexerBase
{
    public SubIndexer(
        ConcurrentDictionary<IndexKey, IndexKeyNum> keysToNumbers, ConcurrentDictionary<IndexKeyNum, IndexKey> numbersToKeys) 
        : base(keysToNumbers, numbersToKeys)
    {
    }

    public void Add(IndexKeyNum keyNumber, int lineNumber)
    {
        var index = Indices.GetOrAdd(keyNumber, static _ => CreateIndexTree());
            
        index.Add(lineNumber);
        CountIndex.Add(lineNumber, Indices);
    }
}

public class Indexer : IndexerBase
{
    private readonly object _componentsLocker = new();

    private readonly ConcurrentDictionary<int, HashSet<IndexKey>> _components = new();

    private readonly ChunkedList<IndexKeyNum> _lineAndKeyIndex = new(16384);

    private int _currentCount = 0;
    public Indexer() 
        : base(new ConcurrentDictionary<IndexKey, IndexKeyNum>(), 
            new ConcurrentDictionary<IndexKeyNum, IndexKey>())
    {
        _keyNumbersValueFactory =  KeyNumbersValueFactory;
    }

    public SubIndexer CreateSubIndexer()
    {
        return new SubIndexer(KeysToNumbers, NumbersToKeys);
    }

    private IndexKeyNum KeyNumbersValueFactory(IndexKey indexKey)
    {
        indexKey.MakeLocalCopy();
        _currentCount++;
        return new IndexKeyNum { KeyNum = _currentCount };
    }

    private readonly Func<IndexKey, IndexKeyNum> _keyNumbersValueFactory;

    public void Add(IndexKey key, int lineNumber)
    {
        var keyCount = _currentCount;
        var keyNumber = KeysToNumbers.GetOrAdd(key, _keyNumbersValueFactory);
        var haveNewKey = _currentCount > keyCount;
        if (haveNewKey)
            NumbersToKeys.TryAdd(keyNumber, key);

        _lineAndKeyIndex.Add(keyNumber);
            
        var index = Indices.GetOrAdd(keyNumber, static _ => CreateIndexTree());
            
        index.Add(lineNumber);
        CountIndex.Add(lineNumber, Indices);

        if (haveNewKey)
            UpdateComponents(key);
    }

    private class ComponentComparer : IEqualityComparer<IndexKey>
    {
        private readonly int _index;

        public ComponentComparer(int index) => _index = index;

        public bool Equals(IndexKey? x, IndexKey? y) =>
            x != null && y != null &&
            x.GetComponent(_index).SequenceEqual(y.GetComponent(_index));

        public int GetHashCode(IndexKey obj) => string.GetHashCode(obj.GetComponent(_index));
    }
    
    private void UpdateComponents(IndexKey key)
    {
        for (var componentIndex = 0; componentIndex < key.ComponentCount; componentIndex++)
        {
            var componentSet = _components.GetOrAdd(componentIndex,
                static index => new HashSet<IndexKey>(new ComponentComparer(index)));

            bool isAdded;
            lock (_componentsLocker)
            {
                isAdded = componentSet.Add(key);
            }

            if (isAdded)
                NewComponentAdded?.Invoke((componentIndex, key));
        }
    }

    public IEnumerable<string> GetAllComponents(int componentNumber)
    {
        if (!_components.TryGetValue(componentNumber, out var componentSet))
            return Enumerable.Empty<string>();

        lock (_componentsLocker)
        {
            return componentSet
                .Select(key => key.GetComponent(componentNumber).ToString()).ToList();
        }
    }

    public event Action<(int compnentNumber, IndexKey key)>? NewComponentAdded;

    public IndexKeyNum GetIndexKeyNum(int index) => _lineAndKeyIndex[index];
}