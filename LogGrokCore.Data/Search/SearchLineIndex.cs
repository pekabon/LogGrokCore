using LogGrokCore.Data.IndexTree;

namespace LogGrokCore.Data.Search
{
    public class SearchLineIndex 
    {
        private readonly IndexTree<int, SimpleLeaf<int>> _searchResult 
            = new IndexTree<int, SimpleLeaf<int>>(32, 
                value => new SimpleLeaf<int>(value, 0));

        private readonly ILineIndex _sourceLineIndex;
        public int Count => _searchResult.Count;
            
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

        // public void Fetch(int start, Span<(int sourceIndex, long offset, int length)> values)
        // {
        //     for (var i = start; i < start + values.Length; i++)
        //     {
        //         values[i] = _sourceLineIndex.GetLine(i);
        //     }
        // }

        public void Add(int sourceLineIndex)
        {
            _searchResult.Add(sourceLineIndex);
        }

    }
}