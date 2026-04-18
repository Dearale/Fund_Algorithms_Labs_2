using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList();
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();


    public virtual void Add(TKey key, TValue value)
    {
        TNode node = CreateNode(key, value);
        if (Root == null)
        {
            Root = node;
            OnNodeAdded(node);
            Count++;
            return;
        }

        TNode? current = Root;
        bool nodeAdded = false;

        while (current != null)
        {
            if (Comparer.Compare(key, current.Key) < 0)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                } else
                {
                    current.Left = node;
                    nodeAdded = true;
                    break;
                }
            } else if (Comparer.Compare(key, current.Key) > 0)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                } else
                {
                    current.Right = node;
                    nodeAdded = true;
                    break;
                }
            } else
            {
                current.Value = node.Value;
                break;
            }
        }
        if (nodeAdded)
        {
            node.Parent = current;
            Count++;
            OnNodeAdded(node);
        }
    }


    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        return true;
    }


    protected virtual void RemoveNode(TNode node)
    {
        if (node.Left == null && node.Right == null)
        {
            Transplant(node, null);
            this.Count--;
            OnNodeRemoved(node.Parent, null, node);
            return;
        }

        if (node.Left == null)
        {
            Transplant(node, node.Right);
            this.Count--;
            OnNodeRemoved(node.Parent, node.Right, node);
            return;
        }

        if (node.Right == null)
        {
            Transplant(node, node.Left);
            this.Count--;
            OnNodeRemoved(node.Parent, node.Left, node);
            return;
        }

        TNode replacement = FindExtreme(node.Left, FindNodeMode.Rightmost);
        node.Key = replacement.Key;
        node.Value = replacement.Value;

        Transplant(replacement, replacement.Left);

        this.Count--;
        OnNodeRemoved(replacement.Parent, replacement.Left, replacement);
    }

    protected enum FindNodeMode { Leftmost, Rightmost }

    protected TNode FindExtreme(TNode node, FindNodeMode mode)
    {
        TNode prev = node;
        TNode? cur = node;
        while (cur != null)
        {
            prev = cur;
            cur = (mode == FindNodeMode.Leftmost) ? cur.Left : cur.Right;
        }
        return prev;
    }

    public virtual bool ContainsKey(TKey key) => TryGetValue(key, out _);

    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }


    #region Hooks

    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }

    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    /// <param name="deletedNode">Узел, который удалили</param>
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child, TNode deletedNode) { }

    #endregion


    #region Helpers
    protected abstract TNode CreateNode(TKey key, TValue value);


    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected void RotateLeft(TNode x)
    {
        if (x == null || x == Root || x.Parent == null) return;
        
        TNode? tmp = x.Left;
        x.Left = x.Parent;
        x.Parent.Right = tmp;
        tmp?.Parent = x.Parent;

        Transplant(x.Parent, x);
        x.Left.Parent = x;
    }

    protected void RotateRight(TNode y)
    {
        if (y == null || y == Root || y.Parent == null) return;

        TNode? tmp = y.Right;
        y.Right = y.Parent;
        y.Parent.Left = tmp;
        tmp?.Parent = y.Parent;

        Transplant(y.Parent, y);
        y.Right.Parent = y;
    }

    protected void RotateBigLeft(TNode x)
    {
        RotateRight(x);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        RotateLeft(y);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        for (int i = 0; i < 2; i++)
            RotateLeft(x);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        for (int i = 0; i < 2; i++)
            RotateRight(y);
    }
    
    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => InOrderTraversal(Root);

    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node)
        => new TreeIterator(node, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);

    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>,
        IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? root;
        private readonly int baseDepth;
        private int depth;
        private readonly TraversalStrategy strategy;

        private TNode? curNode;
        private bool started = false;
        private bool finished = false;

        private TreeEntry<TKey, TValue> current;

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        
        public TreeEntry<TKey, TValue> Current => current;
        object IEnumerator.Current => Current;
        
        public TreeIterator(TNode? root, TraversalStrategy strategy, int baseDepth = 0)
        {
            this.root = root;
            this.strategy = strategy;
            this.baseDepth = baseDepth;
            depth = baseDepth;
        }
        
        public bool MoveNext()
        {
            if (finished) return false;
            bool res = false;
            switch (strategy)
            {
                case TraversalStrategy.PreOrder:
                    res = MoveNextPreOrder();
                    break;
                case TraversalStrategy.InOrder:
                    res = MoveNextInOrder();
                    break;
                case TraversalStrategy.PostOrder:
                    res = MoveNextPostOrder();
                    break;
                case TraversalStrategy.PreOrderReverse:
                    res = MoveNextPostOrderFlipped();
                    break;
                case TraversalStrategy.InOrderReverse:
                    res = MoveNextInOrderFlipped();
                    break;
                case TraversalStrategy.PostOrderReverse:
                    res = MoveNextPreOrderFlipped();
                    break;
            }
            if (res && curNode != null)
            {
                current = new(curNode.Key, curNode.Value, depth);
            }
            return res;
        }

        private bool MoveNextPreOrder()
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    curNode = root;
                    depth = baseDepth;
                    return true;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }

            if (curNode.Left != null)
            {
                curNode = curNode.Left;
                depth += 1;
                return true;
            }
            if (curNode.Right != null)
            {
                curNode = curNode.Right;
                depth += 1;
                return true;
            }
            while (true)
            {
                if (curNode == root)
                {
                    depth = baseDepth;
                    Finish();
                    return false;
                }

                if (curNode!.IsLeftChild && curNode.Parent!.Right != null)
                {
                    curNode = curNode.Parent.Right;
                    return true;
                }

                curNode = curNode.Parent;
                depth--;
            }
        }

        private bool MoveNextPreOrderFlipped()
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    curNode = root;
                    depth = baseDepth;
                    return true;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }

            if (curNode.Right != null)
            {
                curNode = curNode.Right;
                depth += 1;
                return true;
            }
            if (curNode.Left != null)
            {
                curNode = curNode.Left;
                depth += 1;
                return true;
            }
            while (true)
            {
                if (curNode == root)
                {
                    depth = baseDepth;
                    Finish();
                    return false;
                }

                if (curNode!.IsRightChild && curNode.Parent!.Left != null)
                {
                    curNode = curNode.Parent.Left;
                    return true;
                }

                curNode = curNode.Parent;
                depth--;
            }
        }

        public bool MoveNextInOrder()
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    curNode = root;
                    while (curNode.Left != null)
                    {
                        depth++;
                        curNode = curNode.Left;
                    }
                    return true;
                } else
                {
                    Finish();
                    return false;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }
            if (curNode.Right == null)
            {
                while (true)
                {
                    if (curNode!.IsLeftChild)
                    {
                        curNode = curNode.Parent;
                        depth--;
                        return true;
                    } else if (curNode.IsRightChild)
                    {
                        curNode = curNode.Parent;
                        depth--;
                    }
                    if (curNode == root)
                    {
                        depth = baseDepth;
                        return false;
                    }
                }
            } else
            {
                curNode = curNode.Right;
                depth++;
                while (curNode.Left != null)
                {
                    curNode = curNode.Left;
                    depth++;
                }
            }
            return true;
        }

        public bool MoveNextInOrderFlipped()
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    curNode = root;
                    while (curNode.Right != null)
                    {
                        depth++;
                        curNode = curNode.Right;
                    }
                    return true;
                }
                else
                {
                    Finish();
                    return false;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }

            if (curNode.Left == null)
            {
                while (true)
                {
                    if (curNode!.IsRightChild)
                    {
                        curNode = curNode.Parent;
                        depth--;
                        return true;
                    }
                    else if (curNode.IsLeftChild)
                    {
                        curNode = curNode.Parent;
                        depth--;
                    }
                    if (curNode == root)
                    {
                        depth = baseDepth;
                        return false;
                    }
                }
            }
            else
            {
                curNode = curNode.Left;
                depth++;
                while (curNode.Right != null)
                {
                    curNode = curNode.Right;
                    depth++;
                }
            }
            return true;
        }

        private bool MoveNextPostOrder(bool flipped = false)
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    SetCurNodeToLeftRightLeaf(root);
                    return true;
                } else
                {
                    return false;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }

            if (curNode == root) return false;
            if (curNode.IsLeftChild)
            {
                if (curNode.Parent!.Right == null)
                {
                    curNode = curNode.Parent;
                    depth--;
                    return true;
                } else
                {
                    SetCurNodeToLeftRightLeaf(curNode.Parent.Right);
                    return true;

                }
            } else if (curNode.IsRightChild)
            {
                curNode = curNode.Parent;
                depth--;
                return true;
            }
            return true;
        }


        private bool MoveNextPostOrderFlipped()
        {
            if (!started)
            {
                started = true;
                if (root != null)
                {
                    SetCurNodeToRightLeftLeaf(root);
                    return true;
                }
                else
                {
                    return false;
                }
            }

            if (curNode == null)
            {
                Finish();
                return false;
            }

            if (curNode == root) return false;

            if (curNode.IsRightChild)
            {
                if (curNode.Parent!.Left == null)
                {
                    curNode = curNode.Parent;
                    depth--;
                    return true;
                }
                else
                {
                    SetCurNodeToRightLeftLeaf(curNode.Parent.Left);
                    return true;

                }
            }
            else if (curNode.IsLeftChild)
            {
                curNode = curNode.Parent;
                depth--;
                return true;
            }
            return true;
        }
        public void SetCurNodeToLeftRightLeaf(TNode node)
        {
            while (true)
            {
                if (node.Left != null)
                {
                    node = node.Left;
                }
                else if (node.Right != null)
                {
                    node = node.Right;
                }
                else break;
                depth++;
            }
            curNode = node;
        }

        public void SetCurNodeToRightLeftLeaf(TNode node)
        {
            while (true)
            {
                if (node.Right != null)
                {
                    node = node.Right;
                }
                else if (node.Left != null)
                {
                    node = node.Left;
                }
                else break;
                depth++;
            }
            curNode = node;
        }

        private void Finish()
        {
            finished = true;
            current = default;
        }

        public void Reset()
        {
            depth = baseDepth;
            curNode = null;
            started = false;
            finished = false;
            current = default;
        }

        
        public void Dispose()
        {
            curNode = null;
            finished = true;
            current = default;
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Not enough space in array");
        }

        foreach (TreeEntry<TKey, TValue> item in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}