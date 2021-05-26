using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LogGrokCore.Controls
{
    public class Selection : IEnumerable<int>
    {
        private readonly HashSet<int> _indices = new();

        public (int min, int max)? Bounds => _indices.Count == 0 ? null : (_indices.Min(), _indices.Max()); 
       
        public void Add(int index)
        {
            _indices.Add(index);
            Changed?.Invoke();
        }

        public void AddRange(int from, int to)
        {
            foreach (var index in Enumerable.Range(from, to - from))
            {
                Add(index);
            }   
        }

        public void AddRangeToValue(int indexSelectTo)
        {
            if (Bounds is not {min: var min, max: var max})
            {
                Add(indexSelectTo);
            }
            else
            {
                var valueFrom = indexSelectTo > max ? max : min;
                for (var index = Math.Min(valueFrom, indexSelectTo);
                    index <= Math.Max(valueFrom, indexSelectTo);
                    index++)
                {
                    Add(index);
                }
            }

            Changed?.Invoke();
        }

        public void Clear()
        {
            _indices.Clear();
            Changed?.Invoke();
        }

        public void Remove(in int index)
        {
            _indices.Remove(index);
            Changed?.Invoke();
        }

        public void Set(int index)
        {
            _indices.Clear();
            _indices.Add(index);
            Changed?.Invoke();
        }

        public bool Contains(int index) => _indices.Contains(index);

        public event Action? Changed; 

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