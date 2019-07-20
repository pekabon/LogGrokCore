using System;
using LogGrokCore.Data.Virtualization;

namespace LogGrokCore.Data.Index
{
    public class FilteredItemProvider<T> : IItemProvider<T>
    {
       
        public int Count { get; }
        
        public void Fetch(int start, Span<T> values)
        {
            throw new NotImplementedException();
        }
    }
}