using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private void Splay(BstNode<TKey, TValue> newNode)
    {
        BstNode<TKey, TValue> cur = newNode;
        while (cur != null && cur.Parent != null)
        {
            if (cur.IsLeftChild && cur.Parent.IsLeftChild)
            {
                RotateRight(cur);
                RotateRight(cur);
            }
            else if (cur.IsLeftChild && cur.Parent.IsRightChild)
            {
                RotateBigLeft(cur);
            }
            else if (cur.IsRightChild && cur.Parent.IsLeftChild)
            {
                RotateBigRight(cur);
            }
            else if (cur.IsRightChild && cur.Parent.IsRightChild)
            {
                RotateLeft(cur);
                RotateLeft(cur);
            }
            else if (cur.IsLeftChild)
            {
                RotateRight(cur);
            }
            else if (cur.IsRightChild)
            {
                RotateLeft(cur);
            }
            cur = cur.Parent;
        }
    }

    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child, BstNode<TKey, TValue> deletedNode) { }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        BstNode<TKey, TValue>? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            Splay(node);
            return true;
        }
        value = default;
        return false;
    }
    
}
