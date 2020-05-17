using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Controls
{
    public class Selection : IEnumerable<int>
    {
        private readonly HashSet<int> _indices = new HashSet<int>();

        public int Min => _indices.Min();

        public int Max => _indices.Max();

        public void Add(int index) => _indices.Add(index);

        public void Clear() => _indices.Clear();

        public bool Contains(int index) => _indices.Contains(index);

        public void Remove(in int index) => _indices.Remove(index);
        
        public IEnumerator<int> GetEnumerator()
        {
            return _indices.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}