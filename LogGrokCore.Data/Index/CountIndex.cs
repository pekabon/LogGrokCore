using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace LogGrokCore.Data.Index
{
    public class CountIndex
    {
        private const int _granularity = 16384;
        private ImmutableList<List<(IndexKey, int)>> _counts = ImmutableList<List<(IndexKey, int)>>.Empty;

        public IReadOnlyList<List<(IndexKey, int)>> Counts => _counts;

        public int Granularity => _granularity;
        public void Add(int currentIndex, IDictionary<IndexKey, Index> indices)
        {
            if (currentIndex % _granularity == 0)
                MakeCountsSnapshot(indices);
        }

        public void Finish(IDictionary<IndexKey, Index> indices)
        {
            MakeCountsSnapshot(indices);
        }
        
        private void MakeCountsSnapshot(IDictionary<IndexKey, Index> indices)
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