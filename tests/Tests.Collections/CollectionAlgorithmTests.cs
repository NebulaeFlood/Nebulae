using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class CollectionAlgorithmTests
{
    [TestMethod]
    public void CollectionHelpers_Grow_AtSmallAndSaturationBoundaries_IncreasesWithoutExceedingLimit()
    {
        int[] values = [0, 1, 3, 4, 100];

        foreach (int value in values)
        {
            int grown = CollectionHelpers.Grow(value);

            Assert.IsGreaterThan(value, grown, $"Expected growth from {value}, but received {grown}.");
            Assert.IsLessThanOrEqualTo(int.MaxValue, grown);
        }

        Assert.AreEqual(int.MaxValue, CollectionHelpers.Grow(int.MaxValue));
        Assert.AreEqual(10, CollectionHelpers.Grow(9, 10));
        Assert.AreEqual(10, CollectionHelpers.Grow(10, 10));
    }

    [TestMethod]
    public void CollectionHelpers_UnsafeRef_Overloads_ExposeLiveArrayElements()
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
    public void HashHelpers_Modulo_WithCalculatedMultiplier_MatchesRemainder()
    {
        if (!HashHelpers.Bit64)
        {
            return;
        }

        uint[] divisors = [1, 3, 101, 65_521, int.MaxValue];
        uint[] values = [0, 1, 100, 101, 1_000_003, uint.MaxValue];

        foreach (uint divisor in divisors)
        {
            ulong multiplier = HashHelpers.CalculateMultiplier(divisor);

            foreach (uint value in values)
            {
                Assert.AreEqual(value % divisor, HashHelpers.Modulo(value, divisor, multiplier));
            }
        }
    }

    [TestMethod]
    public void HashHelpers_PrimeSelection_AcrossTableBoundary_RespectsInclusiveAndStrictContracts()
    {
        const int lastPrecomputedPrime = 7_199_369;

        int ensured = HashHelpers.EnsurePrime(lastPrecomputedPrime);
        int selected = HashHelpers.GetPrime(lastPrecomputedPrime);

        Assert.AreEqual(lastPrecomputedPrime, ensured);
        Assert.IsGreaterThan(lastPrecomputedPrime, selected);
        Assert.IsTrue(IsPrime(selected));
        Assert.AreNotEqual(0, (selected - 1) % 101);
    }

    [TestMethod]
    public void HashHelpers_Expand_AtNegativeSmallAndMaximumBoundaries_ReportsGrowthOrSaturation()
    {
        Assert.IsFalse(HashHelpers.Expand(-1, out int negativeResult));
        Assert.AreEqual(HashHelpers.MaxSize, negativeResult);

        Assert.IsTrue(HashHelpers.Expand(0, out int initialResult));
        Assert.IsGreaterThan(0, initialResult);
        Assert.IsTrue(IsPrime(initialResult));

        Assert.IsTrue(HashHelpers.Expand(HashHelpers.MaxSize - 1, out int finalGrowth));
        Assert.AreEqual(HashHelpers.MaxSize, finalGrowth);

        Assert.IsFalse(HashHelpers.Expand(HashHelpers.MaxSize, out int saturatedResult));
        Assert.AreEqual(HashHelpers.MaxSize, saturatedResult);
    }

    private static bool IsPrime(int candidate)
    {
        if (candidate < 2)
        {
            return false;
        }

        for (int divisor = 2; (long)divisor * divisor <= candidate; divisor++)
        {
            if (candidate % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
