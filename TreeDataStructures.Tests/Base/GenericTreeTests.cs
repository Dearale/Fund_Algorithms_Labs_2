using System.Reflection;
using System.Runtime.InteropServices;
using TreeDataStructures.Implementations.AVL;
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Implementations.RedBlackTree;
using TreeDataStructures.Implementations.Splay;
using TreeDataStructures.Implementations.Treap;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Tests.Base;

[TestFixture(typeof(BinarySearchTree<int, string>))]
[TestFixture(typeof(AvlTree<int, string>))]
[TestFixture(typeof(RedBlackTree<int, string>))]
[TestFixture(typeof(SplayTree<int, string>))]
[TestFixture(typeof(Treap<int, string>))]
public abstract class GenericTreeTests<TTree> where TTree : ITree<int, string>, new()
{
    private TTree _tree;
    
    [SetUp]
    public void Setup()
    {
        _tree = new TTree();
    }
    
    #region Базовые операции (IDictionary)
    
    [Test]
    public void Test_InsertAndCount()
    {
        _tree.Add(5, "Five");
        _tree.Add(3, "Three");
        _tree.Add(7, "Seven");

        Assert.Multiple(() =>
        {
            Assert.That(_tree.Count, Is.EqualTo(3));
            Assert.That(_tree.ContainsKey(5), Is.True);
            Assert.That(_tree.ContainsKey(99), Is.False);
        });
    }
    
    [Test]
    public void Test_UpdateExistingKey()
    {
        _tree.Add(10, "Initial");
        _tree[10] = "Updated"; // Тест индексатора set
        
        Assert.Multiple(() =>
        {
            Assert.That(_tree.Count, Is.EqualTo(1));
            Assert.That(_tree[10], Is.EqualTo("Updated"));
        });
    }
    
    [Test]
    public void Test_TryGetValue()
    {
        _tree.Add(10, "Ten");
        
        bool found = _tree.TryGetValue(10, out var val);
        bool notFound = _tree.TryGetValue(99, out var nullVal);
        
        Assert.Multiple(() =>
        {
            Assert.That(found, Is.True);
            Assert.That(val, Is.EqualTo("Ten"));
            Assert.That(notFound, Is.False);
        });
    }
    
    [Test]
    public void Test_Indexer_Throws_OnMissingKey()
    {
        Assert.Throws<KeyNotFoundException>(() =>
        {
            string _ = _tree[999];
        });
    }
    
    [Test]
    public void Test_Clear()
    {
        _tree.Add(1, "1");
        _tree.Add(2, "2");
        _tree.Clear();
        
        Assert.Multiple(() =>
        {
            Assert.That(_tree.Count, Is.EqualTo(0));
            Assert.That(_tree.InOrder(), Is.Empty);
        });
    }
    
    [Test]
    public void Test_Keys_Values_Collections()
    {
        Dictionary<int, string> data = new Dictionary<int, string> { { 5, "A" }, { 3, "B" }, { 7, "C" } };
        foreach (var kvp in data) _tree.Add(kvp.Key, kvp.Value);
        
        // Keys и Values в BST возвращаются в порядке возрастания (InOrder)
        List<int> expectedKeys = data.Keys.OrderBy(x => x).ToList();
        List<string> expectedValues = expectedKeys.Select(k => data[k]).ToList();
        
        Assert.Multiple(() =>
        {
            Assert.That(_tree.Keys, Is.EquivalentTo(expectedKeys));
            Assert.That(_tree.Values, Is.EquivalentTo(expectedValues));
        });
    }
    
    #endregion
    
    #region Удаление
    
    [Test]
    public void Test_Remove_Leaf_And_InternalNodes()
    {
        //      50
        //    /    \
        //  30      70
        //  / \    /  \
        // 20 40  60  80
        int[] keys = new[] { 50, 30, 70, 20, 40, 60, 80 };
        foreach (var k in keys) _tree.Add(k, k.ToString());
        
        // 1. Удаление листа
        Assert.That(_tree.Remove(20), Is.True);
        Assert.That(_tree.ContainsKey(20), Is.False);
        
        // 2. Удаление узла с одним ребенком (если бы мы удалили 30 после 20)
        Assert.That(_tree.Remove(30), Is.True);
        Assert.That(_tree.ContainsKey(30), Is.False);
        
        // 3. Удаление корня (узла с двумя детьми)
        Assert.That(_tree.Remove(50), Is.True);
        Assert.That(_tree.ContainsKey(50), Is.False);
        
        Assert.That(_tree.Count, Is.EqualTo(4));
        
        // Проверка, что дерево осталось валидным BST
        List<int> remaining = _tree.InOrder().Select(x => x.Key).ToList();
        Assert.That(remaining, Is.Ordered);
    }
    
    #endregion
    
    #region Обходы (Traversals)
    
    /// <summary>
    /// Тест проверяет классические порядки обхода.
    /// Для BST:
    /// Root=10, Left=5, Right=15
    /// InOrder: 5, 10, 15
    /// PreOrder: 10, 5, 15
    /// PostOrder: 5, 15, 10
    /// </summary>
    [Test]
    public void Test_Traversals_Order()
    {
        _tree.Add(10, "Root");
        _tree.Add(5, "Left");
        _tree.Add(15, "Right");
        
        int[] inOrder = _tree.InOrder().Select(x => x.Key).ToArray();
        int[] preOrder = _tree.PreOrder().Select(x => x.Key).ToArray();
        int[] postOrder = _tree.PostOrder().Select(x => x.Key).ToArray();


        Assert.Multiple(() =>
        {
            Assert.That(inOrder, Is.EqualTo(new[] { 5, 10, 15 }), "InOrder failed");
            Assert.That(preOrder, Is.EqualTo(new[] { 10, 5, 15 }), "PreOrder failed");
            Assert.That(postOrder, Is.EqualTo(new[] { 5, 15, 10 }), "PostOrder failed");
        });
    }
    
    [Test]
    public void Test_Reverse_Traversals()
    {
        _tree.Add(10, "Root");
        _tree.Add(5, "Left");
        _tree.Add(15, "Right");
        
        int[] inOrderRev = _tree.InOrderReverse().Select(x => x.Key).ToArray();
        int[] preOrderRev = _tree.PreOrderReverse().Select(x => x.Key).ToArray();
        int[] postOrderRev = _tree.PostOrderReverse().Select(x => x.Key).ToArray();
        
        Assert.Multiple(() =>
        {
            Assert.That(inOrderRev, Is.EqualTo(new[] { 15, 10, 5 }), "InOrderReverse failed");
            Assert.That(preOrderRev, Is.EqualTo(new[] { 15, 5, 10 }), "PreOrderReverse failed");
            Assert.That(postOrderRev, Is.EqualTo(new[] { 10, 15, 5 }), "PostOrderReverse failed");
        });
    }
    
    #endregion
    
    [Test]
    public void Test_RandomData_Consistency()
    {
        Random random = new (123);
        HashSet<int> inserted = new ();
        
        for (int i = 0; i < 500; i++)
        {
            int val = random.Next(-1000, 1000);
            if (inserted.Add(val)) _tree.Add(val, "v");
        }
        
        Assert.That(_tree.Count, Is.EqualTo(inserted.Count));
        
        // Проверка сортировки
        List<int> sortedKeys = _tree.InOrder().Select(x => x.Key).ToList();
        Assert.That(sortedKeys, Is.Ordered);
        Assert.That(sortedKeys, Is.EquivalentTo(inserted));
        
        // Удаляем половину
        List<int> toRemove = inserted.Take(250).ToList();
        foreach (int k in toRemove)
        {
            Assert.That(_tree.Remove(k), Is.True, $"Failed to remove key {k}");
            inserted.Remove(k);
        }
        
        Assert.That(_tree.Count, Is.EqualTo(inserted.Count));
        Assert.That(_tree.InOrder().Select(x => x.Key), Is.Ordered);
    }
    
    
    #region Splay Tests
    
    private void AssertSplayProperty(int expectedKey)
    {
        if (_tree.GetType().Name.StartsWith("SplayTree"))
        {
            Type bstType = _tree.GetType();
            

            FieldInfo? rootField = null;
            Type? currentType = bstType;
            while (currentType != null && rootField == null)
            {
                rootField = currentType.GetField("Root", BindingFlags.NonPublic | BindingFlags.Instance);
                currentType = currentType.BaseType;
            }
            
            if (rootField == null)
            {
                Assert.Fail("Could not find protected field 'Root' via reflection.");
            }
            
            object? rootNode = rootField?.GetValue(_tree);
            
            Assert.That(rootNode, Is.Not.Null, "Root should not be null after operation");
            
            PropertyInfo? keyProperty = rootNode.GetType().GetProperty("Key");
            int actualKey = (int)keyProperty?.GetValue(rootNode)!;
            
            Assert.That(actualKey, Is.EqualTo(expectedKey),
                $"Splay violation: after accessing key {expectedKey}, it must become the Root.");
        }
    }
    
    [Test]
    public void Test_SplayTree_RootMovement()
    {
        _tree.Add(10, "Ten");
        _tree.Add(20, "Twenty");
        _tree.Add(5, "Five");
        AssertSplayProperty(5);
        
        _tree.ContainsKey(20);
        AssertSplayProperty(20);
        
        string _ = _tree[10];
        AssertSplayProperty(10);
    }
    
    #endregion
}