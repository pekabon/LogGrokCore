using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

namespace LogGrokCore.Data.Index
{
    public class CountIndex<TIndex> where TIndex : IIndex<int>
    {
        public const int Granularity = 16384;
        private ImmutableList<List<(IndexKey, int)>> _counts = ImmutableList<List<(IndexKey, int)>>.Empty;

        public IReadOnlyList<List<(IndexKey, int)>> Counts => _counts;

        public void Add(int currentIndex, IDictionary<IndexKey, TIndex> indices)
        {
            if (currentIndex % Granularity == 0 && currentIndex != 0)
                MakeCountsSnapshot(indices);
        }

        public void Finish(IDictionary<IndexKey, TIndex> indices)
        {
            MakeCountsSnapshot(indices);
        }
        
        private void MakeCountsSnapshot(IDictionary<IndexKey, TIndex> indices)
        {
            var snapshotList = new List<(IndexKey, int)>(indices.Count);
            

#pragma warning disable CS8619
            foreach (var (key, value) in indices)
#pragma warning restore CS8619
            {
                snapshotList.Add((key, value.Count));
            }

            _counts = _counts.Add(snapshotList);
        }
    }
}