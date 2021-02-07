using System;
using LogGrokCore.Data.IndexTree;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Search
{
    public class SearchLineIndex : IItemProvider<int>
    {
        private readonly IndexTree<int, SimpleLeaf<int>> _searchResult 
            = new(32, 
                value => new SimpleLeaf<int>(value, 0));

        private readonly ILineIndex _sourceLineIndex;
        public int Count => _searchResult.Count;
        public void Fetch(int start, Span<int> values)
        {
            using var enumerator = _searchResult.GetEnumerableFromIndex(start).GetEnumerator();
            for (var i = 0; i < values.Length; i++)
            {
                enumerator.MoveNext();
                values[i] = enumerator.Current;
            }
        }

        public SearchLineIndex(ILineIndex sourceLineIndex)
        {
            _sourceLineIndex = sourceLineIndex;
        }

        public (int sourceIndex, long offset, int length) GetLine(int index)
        {
            var sourceIndex = _searchResult[index];
            var (offset, length) = _sourceLineIndex.GetLine(sourceIndex);
            return (sourceIndex, offset, length);
        }

        public int GetIndexByOriginalIndex(int originalIndex)
        {
            return _searchResult.FindIndexByValue(originalIndex);
        }

        public int Add(int sourceLineIndex)
        {
            var addedLineIndex = Count;
            _searchResult.Add(sourceLineIndex);
            return addedLineIndex;
        }
    }
}