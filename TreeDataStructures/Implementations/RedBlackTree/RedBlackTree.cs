using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);
    
    private static RbNode<TKey, TValue>? Grandparent(RbNode<TKey, TValue>? node)
    {
        return node?.Parent?.Parent;
    }

    private static RbNode<TKey, TValue>? Uncle(RbNode<TKey, TValue> node)
    {
        if (node.Parent == null) return null;
        if (node.Parent.IsLeftChild) return Grandparent(node)?.Right;
        else return Grandparent(node)?.Left;
    }
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        InsertCase1(newNode);
    }

    private void InsertCase1(RbNode<TKey, TValue> node)
    {
        if (node.Parent == null)
            node.Color = RbColor.Black;
        else
            InsertCase2(node);
    }

    private void InsertCase2(RbNode<TKey, TValue> node)
    {
        if (node.Parent!.Color != RbColor.Black)
            InsertCase3(node);
    }

    private void InsertCase3(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue>? uncle = Uncle(node);
        if (uncle != null && uncle.Color == RbColor.Red)
        {
            node.Parent!.Color = RbColor.Black;
            uncle.Color = RbColor.Black;
            RbNode<TKey, TValue>? grandparent = Grandparent(node);
            grandparent!.Color = RbColor.Red;
            InsertCase1(grandparent);
        } else
        {
            InsertCase4(node);
        }
    }

    private void InsertCase4(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue>? grandparent = Grandparent(node);
        if (node.IsRightChild && node.Parent!.IsLeftChild)
        {
            RotateLeft(node);
            node = node.Left!;
        } else if (node.IsLeftChild && node.Parent!.IsRightChild)
        {
            RotateRight(node);
            node = node.Right!;
        }
        InsertCase5(node);
    }

    private void InsertCase5(RbNode<TKey, TValue> node)
    {
        RbNode<TKey, TValue>? grandparent = Grandparent(node);
        node.Parent!.Color = RbColor.Black;
        grandparent!.Color = RbColor.Red;
        if (node.IsLeftChild && node.Parent.IsLeftChild)
        {
            RotateRight(node.Parent);
        } else
        {
            RotateLeft(node.Parent);
        }
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child, RbNode<TKey, TValue> deletedNode)
    {
        if (deletedNode.Color == RbColor.Black)
        {
            if (child != null && child.Color == RbColor.Red)
            {
                child.Color = RbColor.Black;
            } else
            {
                RemoveCase1(parent, child);
            }
        }
    }

    private void RemoveCase1(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (parent != null)
        {
            RemoveCase2(parent, child);
        }
    }

    private RbNode<TKey, TValue>? Sibling(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
        => parent.Left == child ? parent.Right : parent.Left;

    private void RemoveCase2(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
    {
        RbNode<TKey, TValue>? sibling = Sibling(parent, child);

        if (sibling != null && sibling.Color == RbColor.Red)
        {
            parent.Color = RbColor.Red;
            sibling.Color = RbColor.Black;
            if (sibling.IsLeftChild)
            {
                RotateRight(sibling);
            } else
            {
                RotateLeft(sibling);
            }
        }
        RemoveCase3(parent, child);
    }

    private void RemoveCase3(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
    {
        RbNode<TKey, TValue>? sibling = Sibling(parent, child);
        if ((parent.Color == RbColor.Black) &&
            (sibling!.Color == RbColor.Black) &&
            (sibling!.Left == null || sibling!.Left.Color == RbColor.Black) &&
            (sibling!.Right == null || sibling!.Right.Color == RbColor.Black))
        {
            sibling.Color = RbColor.Red;
            RemoveCase1(parent.Parent , parent);
        } else
        {
            RemoveCase4(parent, child);
        }
    }

    private void RemoveCase4(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
    {
        RbNode<TKey, TValue>? sibling = Sibling(parent, child);
        if ((parent.Color == RbColor.Red) &&
            (sibling!.Color == RbColor.Black) &&
            (sibling!.Left == null || sibling!.Left.Color == RbColor.Black) &&
            (sibling!.Right == null || sibling!.Right.Color == RbColor.Black))
        {
            sibling.Color = RbColor.Red;
            parent.Color = RbColor.Black;
        } else
        {
            RemoveCase5(parent, child);
        }
    }

    private void RemoveCase5(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
    {
        RbNode<TKey, TValue>? sibling = Sibling(parent, child);
        if (sibling!.Color == RbColor.Black)
        {
            if ((sibling!.IsRightChild) &&
                (sibling!.Left != null && sibling!.Left.Color == RbColor.Red) &&
                (sibling!.Right == null || sibling!.Right.Color == RbColor.Black))
            {
                sibling.Color = RbColor.Red;
                sibling.Left.Color = RbColor.Black;
                RotateRight(sibling.Left);
            } else if ((sibling!.IsLeftChild) &&
                        (sibling!.Left == null || sibling!.Left.Color == RbColor.Black) &&
                        (sibling!.Right != null && sibling!.Right.Color == RbColor.Red))
            {
                sibling.Color = RbColor.Red;
                sibling.Right.Color = RbColor.Black;
                RotateLeft(sibling.Right);
            }
        }
        RemoveCase6(parent, child);
    }

    private void RemoveCase6(RbNode<TKey, TValue> parent, RbNode<TKey, TValue>? child)
    {
        RbNode<TKey, TValue>? sibling = Sibling(parent, child);
        sibling!.Color = parent!.Color;
        parent.Color = RbColor.Black;

        if (sibling.IsRightChild)
        {
            sibling.Right!.Color = RbColor.Black;
            RotateLeft(sibling);
        } else
        {
            sibling.Left!.Color = RbColor.Black;
            RotateRight(sibling);
        }
    }
}