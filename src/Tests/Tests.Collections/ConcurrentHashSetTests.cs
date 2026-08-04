using Nebulae.Collections.Concurrent;

namespace Tests.Collections;

[TestClass]
public sealed class ConcurrentHashSetTests
{
    [TestMethod]
    public void DefaultConstructor_CreatesEmptyMutableSetWithDefaultComparer()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.AreSame(EqualityComparer<int>.Default, set.Comparer);
        Assert.IsFalse(((ICollection<int>)set).IsReadOnly);
    }

    [TestMethod]
    public void ItemsConstructor_DeduplicatesWithComparerAndTryGetValueReturnsStoredInstance()
    {
        var comparer = new KeyedValueComparer();
        var stored = new KeyedValue("alpha", 1);
        var equalValue = new KeyedValue("ALPHA", 2);
        var set = new ConcurrentHashSet<KeyedValue>([stored, equalValue], comparer);

        bool found = set.TryGetValue(equalValue, out KeyedValue? actual);

        Assert.AreSame(comparer, set.Comparer);
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(found);
        Assert.AreSame(stored, actual);
    }

    [TestMethod]
    public void Constructors_InvalidConcurrencyItemsAndNullElementsThrowExactExceptions()
    {
        ArgumentOutOfRangeException concurrencyLevel = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ConcurrentHashSet<int>(0, 1));
        ArgumentNullException items = Assert.ThrowsExactly<ArgumentNullException>(
            () => new ConcurrentHashSet<int>((IEnumerable<int>)null!));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => new ConcurrentHashSet<string>(["valid", null!]));

        Assert.AreEqual("concurrencyLevel", concurrencyLevel.ParamName);
        Assert.AreEqual("items", items.ParamName);
    }

    [TestMethod]
    public void AddContainsRemoveAndTryGetValue_KeepSetStateConsistent()
    {
        var set = new ConcurrentHashSet<int>();

        Assert.IsTrue(set.Add(10));
        Assert.IsFalse(set.Add(10));
        Assert.IsTrue(set.Contains(10));
        Assert.IsTrue(set.TryGetValue(10, out int actual));
        Assert.AreEqual(10, actual);
        Assert.AreEqual(1, set.Count);
        Assert.IsFalse(set.IsEmpty);

        Assert.IsTrue(set.Remove(10));
        Assert.IsFalse(set.Remove(10));
        Assert.IsFalse(set.Contains(10));
        Assert.IsFalse(set.TryGetValue(10, out _));
        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
    }

    [TestMethod]
    public void ICollectionAdd_UsesSetSemantics()
    {
        ConcurrentHashSet<int> concreteSet = [];
        ICollection<int> set = concreteSet;

        set.Add(1);
        set.Add(1);

        Assert.HasCount(1, set);
        Assert.Contains(1, set);
    }

    [TestMethod]
    public void NullReferenceItems_AreRejectedByMutationsAndTreatedAsAbsentByContains()
    {
        var set = new ConcurrentHashSet<string>();

        Assert.IsFalse(set.Contains(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.Add(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.Remove(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => set.TryGetValue(null!, out _));
    }

    [TestMethod]
    public void CopyToAndToArray_ReturnAllElementsWithoutDependingOnHashOrder()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);
        int[] destination = [0, 0, 0, 0, 0];

        set.CopyTo(destination, 1);

        int[] expected = [1, 2, 3];
        CollectionAssert.AreEquivalent(expected, destination[1..4]);
        CollectionAssert.AreEquivalent(expected, set.ToArray());
    }

    [TestMethod]
    public void CopyTo_InvalidArgumentsThrowExactExceptions()
    {
        var set = new ConcurrentHashSet<int>([1, 2, 3]);

        ArgumentNullException nullArray = Assert.ThrowsExactly<ArgumentNullException>(
            () => set.CopyTo(null!, 0));
        ArgumentOutOfRangeException negativeIndex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => set.CopyTo(new int[3], -1));
        ArgumentException insufficientSpace = Assert.ThrowsExactly<ArgumentException>(
            () => set.CopyTo(new int[3], 1));

        Assert.AreEqual("array", nullArray.ParamName);
        Assert.AreEqual("arrayIndex", negativeIndex.ParamName);
        Assert.AreEqual("arrayIndex", insufficientSpace.ParamName);
    }

    [TestMethod]
    public void Clear_RemovesEveryElementAndSetCanBeReused()
    {
        var set = new ConcurrentHashSet<int>(Enumerable.Range(0, 100));

        set.Clear();

        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.IsEmpty(set.ToArray());
        Assert.IsTrue(set.Add(100));
        Assert.IsTrue(set.Contains(100));
    }

    [TestMethod]
    public void Enumerator_InPlaceAdditionBeforeTraversalCanBeObserved()
    {
        var set = new ConcurrentHashSet<int>(1, 31) { 1 };
        using IEnumerator<int> enumerator = set.GetEnumerator();

        set.Add(2);
        List<int> observed = ReadRemaining(enumerator);

        int[] expected = [1, 2];
        CollectionAssert.AreEquivalent(expected, observed);
    }

    [TestMethod]
    public void Enumerator_TableReplacementIsNotObservedUntilReset()
    {
        var set = new ConcurrentHashSet<int>(1, 31) { 1 };
        using IEnumerator<int> enumerator = set.GetEnumerator();

        set.Clear();
        set.Add(2);
        List<int> retainedTable = ReadRemaining(enumerator);

        int[] expectedRetainedTable = [1];
        CollectionAssert.AreEquivalent(expectedRetainedTable, retainedTable);

        enumerator.Reset();
        List<int> currentTable = ReadRemaining(enumerator);

        int[] expectedCurrentTable = [2];
        CollectionAssert.AreEquivalent(expectedCurrentTable, currentTable);
    }

    [TestMethod]
    public void ConstantHashCollisionsAndGrowth_PreserveEveryDistinctValue()
    {
        var set = new ConcurrentHashSet<int>(1, 1, new ConstantHashComparer());

        for (int value = 0; value < 200; value++)
        {
            Assert.IsTrue(set.Add(value));
        }

        Assert.AreEqual(200, set.Count);
        CollectionAssert.AreEquivalent(Enumerable.Range(0, 200).ToArray(), set.ToArray());
    }

    [TestMethod]
    public async Task ConcurrentDuplicateAdds_StoreOneValueAndReportOneSuccess()
    {
        const int operationCount = 64;
        var set = new ConcurrentHashSet<int>();
        var results = new bool[operationCount];

        await RunConcurrently(operationCount, index => results[index] = set.Add(42));

        Assert.AreEqual(1, results.Count(static result => result));
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(42));
    }

    [TestMethod]
    public async Task ConcurrentDistinctAddsAndRemoves_PreserveAllCompletedStateChanges()
    {
        const int operationCount = 128;
        var set = new ConcurrentHashSet<int>(4, 3);

        await RunConcurrently(operationCount, index => set.Add(index));

        Assert.AreEqual(operationCount, set.Count);
        CollectionAssert.AreEquivalent(Enumerable.Range(0, operationCount).ToArray(), set.ToArray());

        await RunConcurrently(operationCount / 2, index => set.Remove(index * 2));

        int[] expected = [.. Enumerable.Range(0, operationCount).Where(static value => (value & 1) is 1)];
        Assert.AreEqual(expected.Length, set.Count);
        CollectionAssert.AreEquivalent(expected, set.ToArray());
    }

    private static List<int> ReadRemaining(IEnumerator<int> enumerator)
    {
        var values = new List<int>();

        while (enumerator.MoveNext())
        {
            values.Add(enumerator.Current);
        }

        return values;
    }

    private static async Task RunConcurrently(int operationCount, Action<int> operation)
    {
        using var startGate = new ManualResetEventSlim();
        Task[] operations =
        [
            .. Enumerable.Range(0, operationCount)
            .Select(index => Task.Run(() =>
            {
                startGate.Wait();
                operation(index);
            }))
        ];

        startGate.Set();
        await Task.WhenAll(operations).WaitAsync(TimeSpan.FromSeconds(10));
    }

    private sealed record KeyedValue(string Key, int Id);

    private sealed class KeyedValueComparer : IEqualityComparer<KeyedValue>
    {
        public bool Equals(KeyedValue? left, KeyedValue? right)
        {
            return StringComparer.OrdinalIgnoreCase.Equals(left?.Key, right?.Key);
        }

        public int GetHashCode(KeyedValue value)
        {
            return StringComparer.OrdinalIgnoreCase.GetHashCode(value.Key);
        }
    }

    private sealed class ConstantHashComparer : IEqualityComparer<int>
    {
        public bool Equals(int left, int right) => left == right;

        public int GetHashCode(int value) => 0;
    }
}
