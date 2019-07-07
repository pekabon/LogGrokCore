using System;
using System.Runtime.InteropServices.ComTypes;

namespace LogGrokCore.Data.Index
{
    public class TreeNode
    {
        public int Occupancy;
        public static Node Empty(int nodeSize) => 
            new Node(new int[nodeSize], new TreeNode[nodeSize + 1]);

        public TreeNode Insert(int newKey, int newValue, Func<Node> leafFactory) 
        {
            throw new NotImplementedException();
        }

        public virtual (TreeNode? newNode, int) InsertCore(int newKey, int newValue, Func<Node> leafFactory)
        {
            switch (this)
            {
                
                case Leaf leaf:
                    
                    break;
            }

            return (null, 0);
        }
    }

    public class Node : TreeNode
    {
        public int[] Keys { get; }
        public TreeNode[] Nodes { get; }

        public Node(int[] keys, TreeNode[] nodes)
        {
            Keys = keys;
            Nodes = nodes;
        }

        public void Deconstruct(out int[] keys, out TreeNode[] nodes)
        {
            keys = Keys;
            nodes = Nodes;
        }

        public override (TreeNode? newNode, int) InsertCore(int newKey, int newValue, Func<Node> leafFactory)
        {
            if (Occupancy == 0)
            {
                var newLeaf = leafFactory();
                _ = newLeaf.InsertCore(newKey, newValue, leafFactory);
                Keys[0] = newKey;
                Nodes[0] = newLeaf;
                Occupancy = 1;
                return (null, 0);
            }

            if (Occupancy == Nodes.Length)
            {
                var currentIndex = Occupancy - 1;
                var targetNode = Nodes[currentIndex];
                var (newNode, seniorKey) = targetNode.InsertCore(newKey, newValue, leafFactory);
                if (newNode == null) return (null, seniorKey);
                var newHead = Empty(Occupancy - 1);
                newHead.Keys[0] = newKey;
                newHead.Nodes[0] = newNode;
                newHead.Occupancy = 1;
                return (newHead, seniorKey);
            }

            {
                var currentIndex = Occupancy - 1;
                var targetNode = Nodes[currentIndex];
                var (newNode, _) = targetNode.InsertCore(newKey, newValue, leafFactory);
                switch (newNode)
                {
                    case null:
                        Keys[currentIndex] = newKey;
                        break;
                    case var _ when Occupancy < Nodes.Length - 1:
                        Keys[currentIndex + 1] = newKey;
                        Nodes[currentIndex + 1] = newNode;
                        Occupancy++;
                        break;
                    default:
                        Nodes[currentIndex + 1] = newNode;
                        Occupancy++;
                        break;
                }
                return (null, 0);
            }
        }
    }

    public class Leaf : TreeNode
    {
       
    }

    public struct LeafPayload<TKey, TValue, TRange>
    {
        public TKey[] Keys;
        public TValue[] Values;
        public TRange[] RangeLength;
    }
}