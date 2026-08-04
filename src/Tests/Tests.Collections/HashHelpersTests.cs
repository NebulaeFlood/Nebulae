using Nebulae.Collections;

namespace Tests.Collections;

[TestClass]
public sealed class HashHelpersTests
{
    [TestMethod]
    [DataRow(-1, false)]
    [DataRow(0, false)]
    [DataRow(1, false)]
    [DataRow(2, true)]
    [DataRow(3, true)]
    [DataRow(4, false)]
    [DataRow(97, true)]
    [DataRow(99, false)]
    public void IsPrime_ClassifiesBoundaryAndRepresentativeValues(int candidate, bool expected)
    {
        Assert.AreEqual(expected, HashHelpers.IsPrime(candidate));
    }

    [TestMethod]
    [DataRow(0, 3)]
    [DataRow(3, 3)]
    [DataRow(4, 7)]
    [DataRow(8, 11)]
    public void EnsurePrime_ReturnsTheFirstConfiguredPrimeAtOrAboveTheRequestedSize(int size, int expected)
    {
        Assert.AreEqual(expected, HashHelpers.EnsurePrime(size));
    }

    [TestMethod]
    [DataRow(0, 3)]
    [DataRow(3, 7)]
    [DataRow(7, 11)]
    [DataRow(10, 11)]
    public void GetPrime_ReturnsASuitablePrimeStrictlyAboveTheMinimum(int minimum, int expected)
    {
        Assert.AreEqual(expected, HashHelpers.GetPrime(minimum));
    }

    [TestMethod]
    [DataRow(-1, false, HashHelpers.MaxSize)]
    [DataRow(0, true, 3)]
    [DataRow(3, true, 7)]
    [DataRow(4, true, 11)]
    [DataRow(HashHelpers.MaxSize, false, HashHelpers.MaxSize)]
    public void Expand_ReportsWhetherCapacityCanGrow(int size, bool expectedResult, int expectedSize)
    {
        bool result = HashHelpers.Expand(size, out int newSize);

        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(expectedSize, newSize);
    }

    [TestMethod]
    [DataRow(0U, 3U)]
    [DataRow(1U, 3U)]
    [DataRow(1234567890U, 31U)]
    [DataRow(uint.MaxValue, 7199369U)]
    public void Modulo_WithCalculatedMultiplierMatchesRemainderOperator(uint value, uint divisor)
    {
        ulong multiplier = HashHelpers.CalculateMultiplier(divisor);

        Assert.AreEqual(value % divisor, HashHelpers.Modulo(value, divisor, multiplier));
    }
}
