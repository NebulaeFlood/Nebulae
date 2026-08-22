using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class CollectorTests
{
    [TestMethod]
    public void Collector_CollectRangeOverloads_WhenGrowthIsRequired_AppendInSourceOrder()
    {
        var collector = new Collector<int>(1);
        collector.Collect(1);

        collector.CollectRange((ICollection<int>)[2, 3]);
        collector.CollectRange(Enumerate(4, 5));
        collector.CollectRange((ReadOnlySpan<int>)[6, 7]);
        int[] expected = [1, 2, 3, 4, 5, 6, 7];

        Assert.AreEqual(7, collector.Count);
        Assert.IsFalse(collector.IsEmpty);
        CollectionAssert.AreEqual(expected, collector.ToArray());
    }

    [TestMethod]
    public void Collector_Indexer_IndexBetweenCountAndCapacity_ReturnsBackingSlot()
    {
        var collector = new Collector<int>(3);

        Assert.AreEqual(0, collector.Count);
        Assert.AreEqual(0, collector[2]);
    }

    [TestMethod]
    public void ValueCollector_CollectRangeOverloads_WhenGrowthIsRequired_AppendInSourceOrder()
    {
        var collector = new ValueCollector<int>(1);
        collector.Collect(1);

        collector.CollectRange((ICollection<int>)[2, 3]);
        collector.CollectRange(Enumerate(4, 5));
        collector.CollectRange((ReadOnlySpan<int>)[6, 7]);
        int[] expected = [1, 2, 3, 4, 5, 6, 7];

        Assert.AreEqual(7, collector.Count);
        Assert.IsFalse(collector.IsEmpty);
        CollectionAssert.AreEqual(expected, collector.ToArray());
    }

    [TestMethod]
    public void ValueCollector_Indexer_IndexBetweenCountAndCapacity_ReturnsBackingSlot()
    {
        var collector = new ValueCollector<int>(3);

        Assert.AreEqual(0, collector.Count);
        Assert.AreEqual(0, collector[2]);
    }

    private static IEnumerable<int> Enumerate(params int[] values)
    {
        foreach (int value in values)
        {
            yield return value;
        }
    }
}
