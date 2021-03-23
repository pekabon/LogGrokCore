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

        public void AddRangeToValue(int selectedValue)
        {
            if (Bounds is not {min: var min, max: var max})
            {
                Add(selectedValue);
            }
            else
            {
                var valueFrom = selectedValue > max ? max : min;
                for (var index = Math.Min(valueFrom, selectedValue);
                    index <= Math.Max(valueFrom, selectedValue);
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