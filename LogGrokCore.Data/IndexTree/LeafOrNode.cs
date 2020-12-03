using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public abstract class LeafOrNode<T, TLeaf> : ITreeNode<T>
        where TLeaf : class, ITreeNode<T>
    {
        public abstract T FirstValue { get; }
        public abstract int MinIndex { get; }
        public abstract IEnumerable<T> GetEnumerableFromIndex(int index);
        public abstract T GetValue(int index);
        public abstract (int index, TLeaf leaf) FindByValue(T value);
    }
}