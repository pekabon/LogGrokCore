using System;
using System.Collections.Generic;
using System.Linq;
using LogGrokCore.Data.Index;

namespace LogGrokCore.Data.IndexTree
{
    public class IndexTree<T, TLeaf> : IIndex<T>
        where TLeaf : LeafOrNode<T, TLeaf>, ILeaf<T, TLeaf>, ITreeNode<T>, IEnumerable<T>
        where T : IComparable<T>
    {
        private readonly int _nodeCapacity;
        private TLeaf? _currentLeaf;
        private int _count;
        private LeafOrNode<T, TLeaf>? _head;
        private readonly Func<T, TLeaf> _createFirstLeaf;

        public IndexTree(int nodeCapacity, Func<T, TLeaf> createFirstLeaf)
        {
            _nodeCapacity = nodeCapacity;
            _createFirstLeaf = createFirstLeaf;
            _head = _currentLeaf;
        }

        public void Add(T value)
        {
            if (_currentLeaf != null)
            {
                var newLeaf = _currentLeaf.Add(value, _count);
                if (newLeaf != null)
                    OnNewLeafCreated(newLeaf);
            }
            else
            {
                _currentLeaf = _createFirstLeaf(value);
                _head = _currentLeaf;
            }
            _count++;
        }

        public int Count => _count;
        
        public IEnumerable<T> GetEnumerableFromIndex(int index)
        {
            return _head == null ? Enumerable.Empty<T>() : _head.GetEnumerableFromIndex(index);
        }

        public IEnumerable<T> GetEnumerableFromValue(T value)
        {
            return _head == null ? Enumerable.Empty<T>() : _head.GetEnumerableFromValue(value);
        }
        
        private void OnNewLeafCreated(TLeaf newLeaf)
        {
            switch (_head)
            {
                case TreeNode<T, TLeaf> headNode:
                    var newNode = AddToTree(headNode, newLeaf);
                    if (newNode != null) 
                        _head = new TreeNode<T, TLeaf>(_nodeCapacity, _head, newNode);
                    break;
                case TLeaf leaf:
                    var treeHead = new TreeNode<T, TLeaf>(_nodeCapacity, leaf, newLeaf);
                    _head = treeHead;
                    break;
            }
            _currentLeaf = newLeaf;
        }

        private static TreeNode<T, TLeaf>? AddToTree(TreeNode<T, TLeaf> node, TLeaf newLeaf)
        {
            var lastSubNode = node.LastSubnode;
            switch (lastSubNode)
            {
                case TreeNode<T, TLeaf> subNode:
                    var newlyCreated = AddToTree(subNode, newLeaf);
                    return newlyCreated != null ? node.TryAdd(newlyCreated) : null;
                case TLeaf _:
                    var newNode = node.TryAdd(newLeaf);
                    return newNode;
                default:
                    throw new InvalidOperationException();
            }
        }

        public T this[int idx] => GetEnumerableFromIndex(idx).First();
    }
}