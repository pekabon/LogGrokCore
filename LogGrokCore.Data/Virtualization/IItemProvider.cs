using System;
using System.Collections.Generic;

namespace LogGrokCore.Data.Virtualization
{

    public interface IItemProvider<T>
    {
        int Count { get; }

        IList<T> Fetch(int start, int count);
    }
}
