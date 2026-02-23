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
        private readonly TraversalStrategy strategy;

        private Stack<(TNode node, int depth)> stack = new();
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
        }
        
        public bool MoveNext()
        {
            if (finished) return false;
            switch (strategy)
            {
                case TraversalStrategy.PreOrder:
                    return MoveNextPreOrder();
                case TraversalStrategy.InOrder:
                    return MoveNextInOrder();
                case TraversalStrategy.PostOrder:
                    return MoveNextPostOrder();
                case TraversalStrategy.PreOrderReverse:
                    return MoveNextPostOrder(flipped: true);
                case TraversalStrategy.InOrderReverse:
                    return MoveNextInOrder(flipped: true);
                case TraversalStrategy.PostOrderReverse:
                    return MoveNextPreOrder(flipped: true);
            }

            throw new NotImplementedException("Strategy not implemented");
        }

        private bool MoveNextPreOrder(bool flipped = false)
        {
            if (!started)
            {
                started = true;
                if (root != null)
                    stack.Push((root, baseDepth));
            }

            if (stack.Count == 0)
            {
                Finish();
                return false;
            }

            (TNode node, int depth) = stack.Pop();
            current = new(node.Key, node.Value, depth);

            if (flipped)
            {
                if (node.Left != null) stack.Push((node.Left, depth + 1));
                if (node.Right != null) stack.Push((node.Right, depth + 1));
            } else
            {
                if (node.Right != null) stack.Push((node.Right, depth + 1));
                if (node.Left != null) stack.Push((node.Left, depth + 1));
            }

            return true;
        }

        public bool MoveNextInOrder(bool flipped = false)
        {
            if (!started)
            {
                started = true;
                if (flipped) PushRightChain(root, depth: baseDepth);
                else PushLeftChain(root, depth: baseDepth);
            }

            if (stack.Count == 0)
            {
                Finish();
                return false;
            }

            (TNode node, int depth) = stack.Pop();
            current = new(node.Key, node.Value, depth);

            if (flipped)
            {
                if (node.Left != null) PushRightChain(node.Left, depth + 1);
            }
            else
            {
                if (node.Right != null) PushLeftChain(node.Right, depth + 1);
            }

            return true;
        }

        private bool MoveNextPostOrder(bool flipped = false)
        {
            if (!started)
            {
                started = true;
                if (flipped)
                {
                    if (root != null) PushRightLeftChain(root, baseDepth);
                } else
                {
                    if (root != null) PushLeftRightChain(root, baseDepth);
                }
            }

            if (stack.Count == 0)
            {
                Finish();
                return false;
            }

            (TNode node, int depth) = stack.Pop();
            current = new(node.Key, node.Value, depth);

            if (flipped)
            {
                if (node.IsRightChild) PushRightLeftChain(node.Parent!.Left, depth);
            } else
            {
                if (node.IsLeftChild) PushLeftRightChain(node.Parent!.Right, depth);
            }

            return true;
        }

        public void PushLeftRightChain(TNode? node, int depth)
        {
            while (node != null)
            {
                stack.Push((node, depth));
                depth++;
                if (node.Left != null)
                {
                    node = node.Left;
                } else if (node.Right != null)
                {
                    node = node.Right;
                } else
                {
                    node = null;
                }
            }
        }

        public void PushRightLeftChain(TNode? node, int depth)
        {
            while (node != null)
            {
                stack.Push((node, depth));
                depth++;
                if (node.Right != null)
                {
                    node = node.Right;
                }
                else if (node.Left != null)
                {
                    node = node.Left;
                }
                else
                {
                    node = null;
                }
            }
        }

        public void PushLeftChain(TNode? node, int depth)
        {
            while (node != null)
            {
                stack.Push((node, depth));
                depth++;
                node = node.Left;
            }
        }

        public void PushRightChain(TNode? node, int depth)
        {
            while (node != null)
            {
                stack.Push((node, depth));
                depth++;
                node = node.Right;
            }
        }

        private void Finish()
        {
            finished = true;
            current = default;
        }

        public void Reset()
        {
            stack.Clear();
            started = false;
            finished = false;
            current = default;
        }

        
        public void Dispose()
        {
            stack.Clear();
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