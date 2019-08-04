using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public interface ITreeNode<T>
    {
        T FirstValue { get; }
        
        int MinIndex { get; }

        IEnumerable<T> GetEnumerableFromIndex(int index);
    }
}