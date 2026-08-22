using Nebulae.Collections.Concurrent;
using System.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class ConcurrentHashSetTests
{
    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public void ConcurrentHashSet_EnumerableConstructor_WithSinglePassSource_EnumeratesOnce()
    {
        var source = new SinglePassEnumerable<int>([1, 2, 3]);

        var set = new ConcurrentHashSet<int>(source);
        int[] expected = [1, 2, 3];

        Assert.AreEqual(1, source.EnumerationCount);
        CollectionAssert.AreEquivalent(expected, set.ToArray());
    }

    [TestMethod]
    public void ConcurrentHashSet_CustomComparer_DeduplicatesAndTryGetValueReturnsStoredInstance()
    {
        var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(set.Add("Alpha"));
        Assert.IsFalse(set.Add("ALPHA"));
        Assert.IsTrue(set.TryGetValue("alpha", out string? actual));

        Assert.AreEqual(1, set.Count);
        Assert.AreEqual("Alpha", actual);
    }

    [TestMethod]
    public void ConcurrentHashSet_ICollectionCopyTo_AtArrayEnd_AllowsOnlyEmptySet()
    {
        var set = new ConcurrentHashSet<int>();
        int[] destination = [];

        ((ICollection<int>)set).CopyTo(destination, destination.Length);

        Assert.IsEmpty(destination);

        set.Add(1);

        Assert.ThrowsExactly<ArgumentException>(
            () => ((ICollection<int>)set).CopyTo(destination, destination.Length));
    }

    [TestMethod]
    public void ConcurrentHashSet_Clear_AfterGrowth_PreservesComparerAndSupportsReuse()
    {
        var comparer = StringComparer.OrdinalIgnoreCase;
        var set = new ConcurrentHashSet<string>(comparer);

        for (int index = 0; index < 256; index++)
        {
            Assert.IsTrue(set.Add($"item-{index}"));
        }

        set.Clear();

        Assert.AreSame(comparer, set.Comparer);
        Assert.AreEqual(0, set.Count);
        Assert.IsTrue(set.IsEmpty);
        Assert.IsTrue(set.Add("Alpha"));
        Assert.IsFalse(set.Add("ALPHA"));
        Assert.IsTrue(set.Contains("alpha"));
    }

    [TestMethod]
    public async Task ConcurrentHashSet_ConcurrentAddsOfSameValue_ExactlyOneSucceeds()
    {
        const int workerCount = 32;
        var set = new ConcurrentHashSet<int>();
        using var start = new ManualResetEventSlim(false);
        CancellationToken cancellationToken = TestContext.CancellationToken;

        Task<bool>[] workers =
        [
            .. Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() =>
            {
                start.Wait(cancellationToken);
                return set.Add(42);
            }, cancellationToken)),
        ];

        start.Set();
        bool[] results = await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

        Assert.AreEqual(1, results.Count(static result => result));
        Assert.AreEqual(1, set.Count);
        Assert.IsTrue(set.Contains(42));
    }

    [TestMethod]
    public async Task ConcurrentHashSet_ConcurrentUniqueAddRemove_AcrossGrowth_PreservesExactSet()
    {
        const int workerCount = 8;
        const int itemCount = 512;
        var set = new ConcurrentHashSet<int>(4, 3, new LowCardinalityHashComparer());
        using var start = new ManualResetEventSlim(false);
        CancellationToken cancellationToken = TestContext.CancellationToken;

        Task[] workers =
        [
            .. Enumerable.Range(0, workerCount)
            .Select(worker => Task.Run(() =>
            {
                start.Wait(cancellationToken);

                for (int value = worker; value < itemCount; value += workerCount)
                {
                    if (!set.Add(value))
                    {
                        throw new InvalidOperationException($"Failed to add unique value {value}.");
                    }

                    if ((value & 1) == 0 && !set.Remove(value))
                    {
                        throw new InvalidOperationException($"Failed to remove value {value}.");
                    }
                }
            }, cancellationToken)),
        ];

        start.Set();
        await Task.WhenAll(workers).WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);

        int[] expected =
        [
            .. Enumerable.Range(0, itemCount)
            .Where(static value => (value & 1) != 0),
        ];

        Assert.AreEqual(expected.Length, set.Count);
        CollectionAssert.AreEquivalent(expected, set.ToArray());
    }

    private sealed class LowCardinalityHashComparer : IEqualityComparer<int>
    {
        public bool Equals(int left, int right) => left == right;

        public int GetHashCode(int value) => value & 3;
    }

    private sealed class SinglePassEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int EnumerationCount => _enumerationCount;

        public IEnumerator<T> GetEnumerator()
        {
            if (Interlocked.Increment(ref _enumerationCount) != 1)
            {
                throw new InvalidOperationException("The source can only be enumerated once.");
            }

            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int _enumerationCount;
    }

    private sealed class TrackingEnumerable<T>(IEnumerable<T> items) : IEnumerable<T>
    {
        public int ActiveEnumeratorCount => _activeEnumeratorCount;

        public IEnumerator<T> GetEnumerator()
        {
            Interlocked.Increment(ref _activeEnumeratorCount);
            return new TrackingEnumerator(this, items.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private int _activeEnumeratorCount;

        private sealed class TrackingEnumerator(TrackingEnumerable<T> owner, IEnumerator<T> inner) : IEnumerator<T>
        {
            public T Current => inner.Current;

            object? IEnumerator.Current => Current;

            public bool MoveNext() => inner.MoveNext();

            public void Reset() => inner.Reset();

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) != 0)
                {
                    return;
                }

                inner.Dispose();
                Interlocked.Decrement(ref owner._activeEnumeratorCount);
            }

            private int _disposed;
        }
    }
}
