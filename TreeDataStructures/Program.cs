// тут можно что-то тестировать
using TreeDataStructures.Implementations.BST;
using TreeDataStructures.Interfaces;

BinarySearchTree<int, string> Tree = new()
{
    { 10, "Root" },
    { 5, "Left" },
    { 15, "Right" },
    { 4, "Left Left" },
    { 6, "Left Right" },
    { 16, "Right Right" },
    { 7, "Left Right Right" },
};

int[] inOrder = Tree.InOrder().Select(x => x.Key).ToArray();
int[] preOrder = Tree.PreOrder().Select(x => x.Key).ToArray();
int[] postOrder = Tree.PostOrder().Select(x => x.Key).ToArray();
foreach (var x in preOrder) Console.WriteLine(x);
Console.WriteLine("---");
foreach (var x in inOrder) Console.WriteLine(x);
Console.WriteLine("---");
foreach (var x in postOrder) Console.WriteLine(x);

Console.WriteLine(5 % -2);