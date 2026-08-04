using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class CollectionHelpersTests
{
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(4)]
    [DataRow(100)]
    public void Grow_NonMaximumValuesIncreaseWithoutOverflow(int value)
    {
        int grown = CollectionHelpers.Grow(value);

        Assert.IsGreaterThan(value, grown);
        Assert.IsLessThanOrEqualTo(int.MaxValue, grown);
    }

    [TestMethod]
    public void Grow_MaximumValueRemainsSaturated()
    {
        Assert.AreEqual(int.MaxValue, CollectionHelpers.Grow(int.MaxValue));
    }

    [TestMethod]
    [DataRow(0, 4, 4)]
    [DataRow(3, 6, 6)]
    [DataRow(4, 6, 6)]
    [DataRow(100, 120, 120)]
    [DataRow(120, 120, 120)]
    public void Grow_WithMaximumNeverExceedsMaximum(int value, int maximum, int expected)
    {
        Assert.AreEqual(expected, CollectionHelpers.Grow(value, maximum));
    }

    [TestMethod]
    public void UnsafeReferences_AliasRequestedArrayElements()
    {
        int[] values = [1, 2, 3];

        ref int first = ref CollectionHelpers.Unsafe.Ref(values);
        ref int second = ref CollectionHelpers.Unsafe.Ref(values, 1);
        ref int third = ref CollectionHelpers.Unsafe.Ref(values, (nuint)2);

        first = 10;
        second = 20;
        third = 30;

        int[] expected = [10, 20, 30];
        CollectionAssert.AreEqual(expected, values);
    }

    [TestMethod]
    public void ThrowHelpers_InvalidCollectionRangesReportTheSuppliedParameter()
    {
        ArgumentException destinationIndex = Assert.ThrowsExactly<ArgumentException>(
            () => CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(3, 3, "arrayIndex"));
        ArgumentException destinationRange = Assert.ThrowsExactly<ArgumentException>(
            () => CollectionHelpers.ThrowHelpers.ThrowIfArrayNotLongEnough(2, 3, 2, "arrayIndex"));
        ArgumentException requiredCount = Assert.ThrowsExactly<ArgumentException>(
            () => CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(2, 3, "requiredCount"));
        ArgumentException sourceRange = Assert.ThrowsExactly<ArgumentException>(
            () => CollectionHelpers.ThrowHelpers.ThrowIfCollectionElementNotEnough(3, 2, 2, "index"));

        Assert.AreEqual("arrayIndex", destinationIndex.ParamName);
        Assert.AreEqual("arrayIndex", destinationRange.ParamName);
        Assert.AreEqual("requiredCount", requiredCount.ParamName);
        Assert.AreEqual("index", sourceRange.ParamName);
    }
}
