using System;
using System.Collections.Generic;
using System.Diagnostics;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.IndexTree
{
    public class TreeNode<T, TLeaf> : LeafOrNode<T, TLeaf> 
        where TLeaf : class, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        where T : IComparable<T>
    {
        private readonly List<LeafOrNode<T, TLeaf>> _subNodes;

        public TreeNode(int nodeCapacity, LeafOrNode<T, TLeaf> first, LeafOrNode<T, TLeaf> second)
        {
            Debug.Assert(nodeCapacity > 1);
            _subNodes = new List<LeafOrNode<T, TLeaf>>(nodeCapacity) {first, second};
        }

        private TreeNode(int nodeCapacity, LeafOrNode<T, TLeaf> first)
        {
            Debug.Assert(nodeCapacity > 1);
            _subNodes = new List<LeafOrNode<T, TLeaf>>(nodeCapacity) {first};
        }

        internal LeafOrNode<T, TLeaf> LastSubNode => _subNodes[^1];
        
        public TreeNode<T, TLeaf>? TryAdd(LeafOrNode<T, TLeaf> node)
        {
            if (_subNodes.Count < _subNodes.Capacity)
                _subNodes.Add(node);
            else
                return new TreeNode<T, TLeaf>(_subNodes.Capacity, node);
            
            return null;
        }

        public override T GetValue(int index) => GetSubNodeByIndex(index).GetValue(index);

        public override IEnumerable<T> GetEnumerableFromIndex(int index) =>
            GetSubNodeByIndex(index).GetEnumerableFromIndex(index);

        public override (int index, TLeaf leaf) FindByValue(T value)
        {
            var index = _subNodes.BinarySearch(0, _subNodes.Count, value,
                static (leafOrNode, t) => leafOrNode.FirstValue.CompareTo(t));
            var subNodeIndex = index >= 0 ? index : ~index - 1;
            return _subNodes[subNodeIndex].FindByValue(value);
        }

        private LeafOrNode<T, TLeaf> GetSubNodeByIndex(int index)
        {
            var found = _subNodes.BinarySearch(0, _subNodes.Count, index,
                static (leafOrNode, t) => leafOrNode.MinIndex.CompareTo(t));
            
            // When i is < 0, ~i is index of the first element, which MinIndex is greater than index.
            // Since we need last element with MinIndex < index, the result is (~i - 1)
            var subNodeIndex = found >= 0 ? found : ~found - 1;
            return _subNodes[subNodeIndex];
        }
        
        public override T FirstValue => _subNodes[0].FirstValue;
        public override int MinIndex => _subNodes[0].MinIndex;
    }
}