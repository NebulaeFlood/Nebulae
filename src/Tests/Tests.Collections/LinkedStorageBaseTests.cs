using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class LinkedStorageBaseTests
{
    [TestMethod]
    public void RawMutations_MaintainHeadTailLinksOrderAndCount()
    {
        var storage = new TestLinkedStorage<int>();

        storage.AddLast(2);
        storage.AddFirst(1);
        storage.AddAfter(1, 4);
        storage.AddBefore(2, 3);

        int[] expectedBeforeDetach = [1, 2, 3, 4];
        Assert.AreEqual(4, storage.Count);
        Assert.IsFalse(storage.IsEmpty);
        Assert.IsTrue(storage.IsBefore(1, 3));
        Assert.IsTrue(storage.IsAfter(3, 1));
        CollectionAssert.AreEqual(expectedBeforeDetach, storage.ToArray());

        Assert.IsTrue(storage.DetachAt(1));

        int[] expectedAfterDetach = [1, 3, 4];
        Assert.AreEqual(3, storage.Count);
        CollectionAssert.AreEqual(expectedAfterDetach, storage.ToArray());

        storage.Reset();

        Assert.AreEqual(0, storage.Count);
        Assert.IsTrue(storage.IsEmpty);
        Assert.IsEmpty(storage.ToArray());
    }

    [TestMethod]
    public void Sort_WithComparisonIsStableAndRepairsBidirectionalLinks()
    {
        var storage = new TestLinkedStorage<SortValue>();
        storage.AddLast(new SortValue(2, "a"));
        storage.AddLast(new SortValue(1, "b"));
        storage.AddLast(new SortValue(2, "c"));
        storage.AddLast(new SortValue(1, "d"));

        storage.Sort(static (left, right) => left.Key.CompareTo(right.Key));

        SortValue[] expected =
        [
            new(1, "b"),
            new(1, "d"),
            new(2, "a"),
            new(2, "c")
        ];
        CollectionAssert.AreEqual(expected, storage.ToArray());
        Assert.IsTrue(storage.HasConsistentLinks());
    }

    [TestMethod]
    public void Sort_WithComparerOrdersAllElements()
    {
        var storage = CreateStorage(4, 1, 3, 2);

        storage.Sort(Comparer<int>.Default);

        int[] expected = [1, 2, 3, 4];
        CollectionAssert.AreEqual(expected, storage.ToArray());
        Assert.IsTrue(storage.HasConsistentLinks());
    }

    [TestMethod]
    public void CopyTo_CopiesInLinkedOrderAtRequestedIndex()
    {
        var storage = CreateStorage(1, 2, 3);
        int[] destination = [0, 0, 0, 0, 0];

        storage.CopyTo(destination, 1);

        int[] expected = [0, 1, 2, 3, 0];
        CollectionAssert.AreEqual(expected, destination);
    }

    [TestMethod]
    public void CopyTo_InvalidArgumentsThrowExactExceptions()
    {
        var storage = CreateStorage(1, 2, 3);

        ArgumentNullException nullArray = Assert.ThrowsExactly<ArgumentNullException>(
            () => storage.CopyTo(null!, 0));
        ArgumentOutOfRangeException negativeIndex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => storage.CopyTo(new int[3], -1));
        ArgumentException insufficientSpace = Assert.ThrowsExactly<ArgumentException>(
            () => storage.CopyTo(new int[3], 1));

        Assert.AreEqual("array", nullArray.ParamName);
        Assert.AreEqual("arrayIndex", negativeIndex.ParamName);
        Assert.AreEqual("arrayIndex", insufficientSpace.ParamName);
    }

    [TestMethod]
    public void Enumerator_TraversesLinkedOrderAndCanReset()
    {
        var storage = CreateStorage(1, 2, 3);
        LinkedStorageBase<int>.Enumerator enumerator = storage.GetEnumerator();

        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(2, enumerator.Current);

        enumerator.Reset();

        int[] expected = [1, 2, 3];
        Assert.IsTrue(enumerator.MoveNext());
        Assert.AreEqual(1, enumerator.Current);
        CollectionAssert.AreEqual(expected, ((IEnumerable<int>)storage).ToArray());
    }

    private static TestLinkedStorage<int> CreateStorage(params int[] values)
    {
        var storage = new TestLinkedStorage<int>();

        foreach (int value in values)
        {
            storage.AddLast(value);
        }

        return storage;
    }

    private readonly record struct SortValue(int Key, string Id);

    private sealed class TestLinkedStorage<T> : LinkedStorageBase<T>
    {
        public void AddAfter(int index, T item) => RawAddAfter(GetNode(index), item);

        public void AddBefore(int index, T item) => RawAddBefore(GetNode(index), item);

        public void AddFirst(T item) => RawAddFirst(item);

        public void AddLast(T item) => RawAddLast(item);

        public bool DetachAt(int index)
        {
            Node node = GetNode(index);
            RawDetach(node);
            return node.Prev is null && node.Next is null;
        }

        public bool HasConsistentLinks()
        {
            Node? previous = null;
            Node? current = head;
            int visited = 0;

            while (current is not null)
            {
                if (!ReferenceEquals(previous, current.Prev))
                {
                    return false;
                }

                previous = current;
                current = current.Next;
                visited++;
            }

            return ReferenceEquals(previous, tail) && visited == count;
        }

        public bool IsAfter(int index, int otherIndex) => GetNode(index).After(GetNode(otherIndex));

        public bool IsBefore(int index, int otherIndex) => GetNode(index).Before(GetNode(otherIndex));

        public void Reset() => RawReset();

        public void Sort(Comparison<T> comparison) => RawSort(comparison);

        public void Sort(IComparer<T> comparer) => RawSort(comparer);

        private Node GetNode(int index)
        {
            Node? current = head;

            for (int currentIndex = 0; currentIndex < index; currentIndex++)
            {
                current = current!.Next;
            }

            return current!;
        }
    }
}
