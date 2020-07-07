using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LogGrokCore.Data.IndexTree
{
    public class TreeNode<T, TLeaf> : LeafOrNode<T, TLeaf> 
        where TLeaf : class, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        where T : IComparable<T>
    {
        private readonly List<LeafOrNode<T, TLeaf>> _subnodes;

        public TreeNode(int nodeCapacity, LeafOrNode<T, TLeaf> first, LeafOrNode<T, TLeaf> second)
        {
            Debug.Assert(nodeCapacity > 1);
            _subnodes = new List<LeafOrNode<T, TLeaf>>(nodeCapacity) {first, second};
        }

        public TreeNode(int nodeCapacity, LeafOrNode<T, TLeaf> first)
        {
            Debug.Assert(nodeCapacity > 1);
            _subnodes = new List<LeafOrNode<T, TLeaf>>(nodeCapacity) {first};
        }

        internal LeafOrNode<T, TLeaf> LastSubnode => _subnodes[_subnodes.Count - 1];
        
        public TreeNode<T, TLeaf>? TryAdd(LeafOrNode<T, TLeaf> node)
        {
            if (_subnodes.Count < _subnodes.Capacity)
                _subnodes.Add(node);
            else
                return new TreeNode<T, TLeaf>(_subnodes.Capacity, node);
            
            return null;
        }

        public override IEnumerable<T> GetEnumerableFromIndex(int index)
        {
            var leafOrNode = _subnodes[0];
            for (var idx = 1; idx < _subnodes.Count; idx++)
            {
                var candidate = _subnodes[idx];
                if (candidate.MinIndex <= index)
                    leafOrNode = candidate;
                else
                    return leafOrNode.GetEnumerableFromIndex(index);
            }

            return leafOrNode.GetEnumerableFromIndex(index);
        }

        public override (int index, TLeaf leaf) FindByValue(T value)
        {
            
            var leafOrNode = _subnodes[0];
            for (var idx = 1; idx < _subnodes.Count; idx++)
            {
                var candidate = _subnodes[idx];
                if (candidate.FirstValue.CompareTo(value) <= 0)
                    leafOrNode = candidate;
                else
                    return leafOrNode.FindByValue(value);
            }

            return leafOrNode.FindByValue(value);
        }

        public override T FirstValue => _subnodes[0].FirstValue;
        public override int MinIndex => _subnodes[0].MinIndex;
    }
}