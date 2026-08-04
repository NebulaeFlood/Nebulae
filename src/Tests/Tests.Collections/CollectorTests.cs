using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class CollectorTests
{
    [TestMethod]
    public void Constructors_ExposeEmptyOrProvidedContents()
    {
        var empty = new Collector<int>();
        int[] source = [1, 2, 3];
        var populated = new Collector<int>(source);

        Assert.AreEqual(0, empty.Count);
        Assert.IsEmpty(empty.ToArray());
        Assert.AreEqual(3, populated.Count);
        CollectionAssert.AreEqual(source, populated.ToArray());
    }

    [TestMethod]
    public void Collect_FromZeroCapacityGrowsAndPreservesInsertionOrder()
    {
        var collector = new Collector<int>(0);

        for (int value = 1; value <= 20; value++)
        {
            collector.Collect(value);
        }

        Assert.AreEqual(20, collector.Count);
        CollectionAssert.AreEqual(Enumerable.Range(1, 20).ToArray(), collector.ToArray());
    }

    [TestMethod]
    public void Views_ExposeOnlyCollectedElementsAndWriteThroughToStorage()
    {
        var collector = new Collector<int>(4);
        collector.Collect(1);
        collector.Collect(2);

        Span<int> span = collector.AsSpan();
        Memory<int> memory = collector.AsMemory();
        span[0] = 10;
        memory.Span[1] = 20;

        int[] expected = [10, 20];
        Assert.AreEqual(2, span.Length);
        Assert.AreEqual(2, memory.Length);
        CollectionAssert.AreEqual(expected, collector.ToArray());
    }

    [TestMethod]
    public void CopyTo_CopiesCollectedElementsAtRequestedIndex()
    {
        var collector = new Collector<int>([1, 2, 3]);
        int[] destination = [0, 0, 0, 0, 0];

        collector.CopyTo(destination, 1);

        int[] expected = [0, 1, 2, 3, 0];
        CollectionAssert.AreEqual(expected, destination);
    }

    [TestMethod]
    public void CopyTo_NullArrayAndNegativeIndexThrowExactExceptions()
    {
        ArgumentNullException nullArray = Assert.ThrowsExactly<ArgumentNullException>(CopyToNullArray);
        ArgumentOutOfRangeException negativeIndex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(CopyToNegativeIndex);

        Assert.AreEqual("array", nullArray.ParamName);
        Assert.AreEqual("arrayIndex", negativeIndex.ParamName);
    }

    [TestMethod]
    public void Enumerator_TraversesCollectedElementsAndCanReset()
    {
        var collector = new Collector<int>([1, 2, 3]);
        Collector<int>.Enumerator enumerator = collector.GetEnumerator();
        var firstPass = new List<int>();

        while (enumerator.MoveNext())
        {
            firstPass.Add(enumerator.Current);
        }

        enumerator.Reset();
        var secondPass = new List<int>();

        while (enumerator.MoveNext())
        {
            secondPass.Add(enumerator.Current);
        }

        int[] expected = [1, 2, 3];
        CollectionAssert.AreEqual(expected, firstPass);
        CollectionAssert.AreEqual(firstPass, secondPass);
    }

    private static void CopyToNullArray()
    {
        var collector = new Collector<int>([1]);
        collector.CopyTo(null!, 0);
    }

    private static void CopyToNegativeIndex()
    {
        var collector = new Collector<int>([1]);
        collector.CopyTo(new int[1], -1);
    }
}
