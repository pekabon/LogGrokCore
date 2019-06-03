using System;
using System.Collections.Generic;

namespace LogGrokCore.Data.Virtualization
{

    public interface IItemProvider<T>
    {
        public int Count { get; }

        public IList<T> Fetch(int start, int count);
    }
}
