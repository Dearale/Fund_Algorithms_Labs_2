using System.Text;
using TreeDataStructures.Core;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private int GetHeight(AvlNode<TKey, TValue>? node) => node != null ? node.Height : 0;
    private int BFactor(AvlNode<TKey, TValue> node) => GetHeight(node.Right) - GetHeight(node.Left);
    private void RecalcHeight(AvlNode<TKey, TValue>? node)
    {
        node?.Height = Math.Max(GetHeight(node.Left), GetHeight(node.Right)) + 1;
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        AvlNode<TKey, TValue>? cur = newNode;

        while (cur != null && cur.Parent != null)
        {
            AvlNode<TKey, TValue> parent = cur.Parent;
            RecalcHeight(parent);
            int parentBFactor = BFactor(parent);

            if (parentBFactor < -1)
            {
                if (BFactor(cur) < 0)
                {
                    RotateRight(cur);
                } else if (BFactor(cur) > 0)
                {
                    RotateBigRight(cur.Right!);

                }
            } else if (parentBFactor > 1)
            {
                if (BFactor(cur) < 0)
                {
                    RotateBigLeft(cur.Left!);
                }
                else if (BFactor(cur) > 0)
                {
                    RotateLeft(cur);
                }
            }
            else if (parentBFactor == 0)
            {
                return;
            }
            RecalcHeight(parent);
            RecalcHeight(cur);
            RecalcHeight(cur.Parent);

            cur = cur.Parent;
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child, AvlNode<TKey, TValue> deletedNode)
    {
        if (parent == null) return;
        AvlNode<TKey, TValue>? cur = parent;

        while (cur != null)
        {
            RecalcHeight(cur);
            int curBFactor = BFactor(cur);
            if (curBFactor < -1)
            {
                if (BFactor(cur.Left!) <= 0)
                {
                    RotateRight(cur.Left!);
                }
                else if (BFactor(cur.Left!) > 0)
                {
                    RotateBigRight(cur.Left!.Right!);

                }
            }
            else if (curBFactor > 1)
            {
                if (BFactor(cur.Right!) < 0)
                {
                    RotateBigLeft(cur.Right!.Left!);
                }
                else if (BFactor(cur.Right!) >= 0)
                {
                    RotateLeft(cur.Right!);
                }
            }
            else if (curBFactor == -1 || curBFactor == 1)
            {
                break;
            }
            RecalcHeight(cur);
            RecalcHeight(cur.Parent);
            if (cur.Parent != null)
            {
                RecalcHeight(cur.Parent.Left);
                RecalcHeight(cur.Parent.Right);
            }

            cur = cur.Parent;
        }
    }
}