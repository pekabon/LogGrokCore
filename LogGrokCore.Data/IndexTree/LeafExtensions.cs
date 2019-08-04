using System.Collections.Generic;

namespace LogGrokCore.Data.IndexTree
{
    public static class LeafExtensions
    {
        public static IEnumerable<T> GetEnumerableFromIndex<T, TLeaf>(this TLeaf leaf, int index)
            where TLeaf : class, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        {
            for (var i = index - leaf.MinIndex; i < leaf.Count; i++)
            {
                yield return leaf[i];
            }

            var next = leaf.Next;
            while (next != null)
            {
                foreach (var value in next)
                {
                    yield return value;
                }

                next = next.Next;
            }
        }
    }
}