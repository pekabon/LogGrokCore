using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogGrokCore.Data.Index
{
   public class CountIndex<TIndex> where TIndex : IIndex<int>
    {
        public const int Granularity = 16384;
        private ImmutableList<List<(IndexKeyNum, int)>> _counts = ImmutableList<List<(IndexKeyNum, int)>>.Empty;
        private readonly IDictionary<IndexKeyNum,TIndex> _indices;
        private bool _isFinished = false;

        public IReadOnlyList<List<(IndexKeyNum, int)>> Counts
        {
            get
            {
                if (_isFinished)
                    return _counts;
                var counts = _counts.Add(MakeCountsSnapshot());
                return _isFinished ? _counts : counts;
            }
        }

        public CountIndex(IDictionary<IndexKeyNum, TIndex> indices)
        {
            _indices = indices;
        }

        public void Add(int currentIndex, IDictionary<IndexKeyNum, TIndex> indices)
        {
            if (currentIndex % Granularity == 0 && currentIndex != 0)
                UpdateCountsSnapshot();
        }

        public void Finish(IDictionary<IndexKeyNum, TIndex> indices)
        {
            UpdateCountsSnapshot();
            _isFinished = true;
        }

        private void UpdateCountsSnapshot()
        {
            _counts = _counts.Add(MakeCountsSnapshot());
        }

        private List<(IndexKeyNum, int)> MakeCountsSnapshot()
        {
            var snapshotList = new List<(IndexKeyNum, int)>(_indices.Count);
            

#pragma warning disable CS8619
            foreach (var (key, value) in _indices)
#pragma warning restore CS8619
            {
                snapshotList.Add((key, value.Count));
            }

            return snapshotList;
        }
    }
}