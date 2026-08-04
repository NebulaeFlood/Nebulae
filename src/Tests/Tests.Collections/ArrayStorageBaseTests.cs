using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class ArrayStorageBaseTests
{
    [TestMethod]
    public void Constructor_NegativeCapacityThrowsForCapacityParameter()
    {
        ArgumentOutOfRangeException exception = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new TestArrayStorage<int>(-1));

        Assert.AreEqual("capacity", exception.ParamName);
    }

    [TestMethod]
    public void RawMutations_PreserveOrderCountAndEmptyStateAcrossGrowth()
    {
        var storage = new TestArrayStorage<int>(0);

        storage.AddLast(2);
        storage.AddFirst(1);
        storage.AddAfter(1, 4);
        storage.AddBefore(2, 3);
        storage.MoveToTail(0);
        storage.MoveToHead(2);
        storage.RemoveAt(1);

        int[] expected = [4, 3, 1];
        Assert.AreEqual(3, storage.Count);
        Assert.IsFalse(storage.IsEmpty);
        CollectionAssert.AreEqual(expected, storage.ToArray());

        storage.Reset();

        Assert.AreEqual(0, storage.Count);
        Assert.IsTrue(storage.IsEmpty);
        Assert.IsEmpty(storage.ToArray());
    }

    [TestMethod]
    public void CopyAndSlice_UseOnlyTheLogicalElementRange()
    {
        var storage = CreateStorage(1, 2, 3, 4);
        int[] completeDestination = new int[4];
        int[] offsetDestination = [0, 0, 0, 0, 0, 0];
        int[] partialDestination = [0, 0, 0, 0];

        storage.CopyTo(completeDestination);
        storage.CopyTo(offsetDestination, 1);
        storage.CopyTo(1, partialDestination, 1, 2);

        int[] expectedComplete = [1, 2, 3, 4];
        int[] expectedOffset = [0, 1, 2, 3, 4, 0];
        int[] expectedPartial = [0, 2, 3, 0];
        int[] expectedSlice = [2, 3];
        CollectionAssert.AreEqual(expectedComplete, completeDestination);
        CollectionAssert.AreEqual(expectedOffset, offsetDestination);
        CollectionAssert.AreEqual(expectedPartial, partialDestination);
        CollectionAssert.AreEqual(expectedSlice, storage.ToArray(1, 2));
    }

    [TestMethod]
    public void RangeMethods_InvalidArgumentsThrowExactExceptions()
    {
        var storage = CreateStorage(1, 2, 3);

        ArgumentNullException nullArray = Assert.ThrowsExactly<ArgumentNullException>(
            () => storage.CopyTo(null!));
        ArgumentOutOfRangeException negativeDestinationIndex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => storage.CopyTo(new int[3], -1));
        ArgumentOutOfRangeException negativeSourceIndex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => storage.ToArray(-1, 1));
        ArgumentException insufficientElements = Assert.ThrowsExactly<ArgumentException>(
            () => storage.ToArray(2, 2));

        Assert.AreEqual("array", nullArray.ParamName);
        Assert.AreEqual("arrayIndex", negativeDestinationIndex.ParamName);
        Assert.AreEqual("index", negativeSourceIndex.ParamName);
        Assert.AreEqual("index", insufficientElements.ParamName);
    }

    [TestMethod]
    public void Enumerator_TraversesLogicalElementsAndCanReset()
    {
        var storage = CreateStorage(1, 2, 3);
        ArrayStorageBase<int>.Enumerator enumerator = storage.GetEnumerator();

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

    private static TestArrayStorage<int> CreateStorage(params int[] values)
    {
        var storage = new TestArrayStorage<int>(values.Length);

        foreach (int value in values)
        {
            storage.AddLast(value);
        }

        return storage;
    }

    private sealed class TestArrayStorage<T>(int capacity) : ArrayStorageBase<T>(capacity)
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
}
