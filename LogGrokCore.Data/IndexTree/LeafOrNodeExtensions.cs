using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public static class LeafOrNodeExtensions
    {
        public static IEnumerable<T> GetEnumerableFromValue<T, TLeaf>(this LeafOrNode<T, TLeaf> leafOrNode, T value)
            where  TLeaf : class, ITreeNode<T>
        {
            var (index , leaf) = leafOrNode.FindByValue(value);
            return leaf.GetEnumerableFromIndex(index);
        }

        public static int GetIndexByValue<T, TLeaf>(this LeafOrNode<T, TLeaf> leafOrNode, T value)
            where  TLeaf : class, ITreeNode<T>
        {
            var (index, _) = leafOrNode.FindByValue(value);
            return index;
        }
    }
}