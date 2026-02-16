namespace TreeDataStructures.Interfaces;

public readonly record struct TreeEntry<TKey, TValue>(TKey Key, TValue Value, int Depth);

public interface ITree<TKey, TValue> : IDictionary<TKey, TValue>
{
    // Прямой порядок
    IEnumerable<TreeEntry<TKey, TValue>>  InOrder();   // Infix
    IEnumerable<TreeEntry<TKey, TValue>>  PreOrder();  // Prefix
    IEnumerable<TreeEntry<TKey, TValue>>  PostOrder(); // Postfix
    
    // Обратный порядок
    IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse();  // Infix Reverse
    IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse(); // Prefix Reverse
    IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse(); // Postfix Reverse
    
    IComparer<TKey> Comparer { get; }
}

    