using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class StorageBaseTests
{
    [TestMethod]
    public void ArrayStorage_RawMutationSequence_FromZeroCapacity_PreservesLogicalOrderAndState()
    {
        var storage = new ArrayStorageProbe<int>(0);

        storage.AddLast(2);
        storage.AddFirst(1);
        storage.AddAfter(1, 4);
        storage.AddBefore(2, 3);
        storage.MoveToHead(2);
        storage.MoveToTail(1);
        storage.RemoveAt(2);
        int[] expected = [3, 2, 1];

        Assert.AreEqual(3, storage.Count);
        Assert.IsFalse(storage.IsEmpty);
        CollectionAssert.AreEqual(expected, storage.ToArray());

        storage.Reset();

        Assert.AreEqual(0, storage.Count);
        Assert.IsTrue(storage.IsEmpty);
        Assert.IsEmpty(storage.ToArray());
    }

    [TestMethod]
    public void ArrayStorage_RangeOperations_AtCollectionEnd_HandleZeroLengthAndExactSlices()
    {
        var storage = new ArrayStorageProbe<int>(0);
        storage.AddLast(10);
        storage.AddLast(20);
        storage.AddLast(30);
        int[] expectedSlice = [20, 30];

        CollectionAssert.AreEqual(expectedSlice, storage.ToArray(1, 2));
        Assert.IsEmpty(storage.ToArray(3, 0));

        storage.CopyTo(3, [], 0, 0);

        int[] destination = [-1, -1, -1, -1, -1];
        storage.CopyTo(1, destination, 2, 2);
        int[] expectedDestination = [-1, -1, 20, 30, -1];
        CollectionAssert.AreEqual(expectedDestination, destination);
    }

    [TestMethod]
    public void LinkedStorage_DetachAndReinsert_AcrossHeadAndTail_PreservesBidirectionalOrder()
    {
        var storage = new LinkedStorageProbe<int>();
        storage.AddLast(1);
        storage.AddLast(2);
        storage.AddLast(3);
        storage.AddLast(4);

        storage.DetachToLast(0);
        storage.DetachToFirst(2);
        int[] expectedForward = [4, 2, 3, 1];
        int[] expectedReverse = [1, 3, 2, 4];

        Assert.AreEqual(4, storage.Count);
        Assert.IsFalse(storage.IsEmpty);
        CollectionAssert.AreEqual(expectedForward, storage.ToArray());
        CollectionAssert.AreEqual(expectedReverse, storage.ToReverseArray());
    }

    [TestMethod]
    public void LinkedStorage_SortOverloads_OnOddLengthThenAppend_PreserveSortedOrderAndEndpoints()
    {
        var comparisonStorage = CreateUnsortedLinkedStorage();
        comparisonStorage.Sort(static (left, right) => left.CompareTo(right));
        comparisonStorage.AddLast(6);
        int[] expectedComparisonForward = [1, 2, 3, 4, 5, 6];
        int[] expectedComparisonReverse = [6, 5, 4, 3, 2, 1];

        CollectionAssert.AreEqual(expectedComparisonForward, comparisonStorage.ToArray());
        CollectionAssert.AreEqual(expectedComparisonReverse, comparisonStorage.ToReverseArray());

        var comparerStorage = CreateUnsortedLinkedStorage();
        comparerStorage.Sort(Comparer<int>.Default);
        comparerStorage.AddFirst(0);
        int[] expectedComparerForward = [0, 1, 2, 3, 4, 5];
        int[] expectedComparerReverse = [5, 4, 3, 2, 1, 0];

        CollectionAssert.AreEqual(expectedComparerForward, comparerStorage.ToArray());
        CollectionAssert.AreEqual(expectedComparerReverse, comparerStorage.ToReverseArray());
    }

    private static LinkedStorageProbe<int> CreateUnsortedLinkedStorage()
    {
        var storage = new LinkedStorageProbe<int>();

        int[] values = [5, 1, 4, 2, 3];

        foreach (int value in values)
        {
            storage.AddLast(value);
        }

        return storage;
    }

    private sealed class ArrayStorageProbe<T>(int capacity) : ArrayStorageBase<T>(capacity)
    {
        public void AddAfter(int index, T item) => RawAddAfter(index, item);

        public void AddBefore(int index, T item) => RawAddBefore(index, item);

        public void AddFirst(T item) => RawAddFirst(item);

        public void AddLast(T item) => RawAddLast(item);

        public void MoveToHead(int index) => RawMoveToHead(index);

        public void MoveToTail(int index) => RawMoveToTail(index);

        public void RemoveAt(int index) => RawRemoveAt(index);

        public void Reset() => RawReset();
    }

    private sealed class LinkedStorageProbe<T> : LinkedStorageBase<T>
    {
        public void AddFirst(T item) => RawAddFirst(item);

        public void AddLast(T item) => RawAddLast(item);

        public void DetachToFirst(int index)
        {
            Node node = GetNode(index);
            RawDetach(node);
            RawAddFirst(node);
        }

        public void DetachToLast(int index)
        {
            Node node = GetNode(index);
            RawDetach(node);
            RawAddLast(node);
        }

        public void Sort(Comparison<T> comparison) => RawSort(comparison);

        public void Sort(IComparer<T> comparer) => RawSort(comparer);

        public T[] ToReverseArray()
        {
            var result = new T[count];
            int index = 0;

            for (Node? node = tail; node is not null; node = node.Prev)
            {
                result[index++] = node.Item;
            }

            return result;
        }

        private Node GetNode(int index)
        {
            Node? node = head;

            for (int currentIndex = 0; currentIndex < index; currentIndex++)
            {
                node = node!.Next;
            }

            return node!;
        }
    }
}
