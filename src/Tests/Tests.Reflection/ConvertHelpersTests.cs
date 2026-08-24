using Nebulae.Reflection;
using System.Globalization;

namespace Tests.Reflection;

[TestClass]
public sealed class ConvertHelpersTests
{
    [TestMethod]
    public void ChangeType_SupportedPrimitiveConversions_ReturnExpectedValues()
    {
        IFormatProvider provider = CultureInfo.InvariantCulture;

        int integer = ConvertHelpers.ChangeType<string, int>("42", provider);
        string? text = ConvertHelpers.ChangeType<int, string>(42, provider);
        bool boolean = ConvertHelpers.ChangeType<string, bool>("true", provider);

        Assert.AreEqual(42, integer);
        Assert.AreEqual("42", text);
        Assert.IsTrue(boolean);
    }

    [TestMethod]
    public void ChangeType_CultureSensitiveDecimal_UsesProvidedCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");

        decimal value = ConvertHelpers.ChangeType<string, decimal>("12,5", culture);

        Assert.AreEqual(12.5m, value);
    }

    [TestMethod]
    [DataRow(typeof(int), typeof(string), true)]
    [DataRow(typeof(char), typeof(int), true)]
    [DataRow(typeof(DateTime), typeof(string), true)]
    [DataRow(typeof(DateTime), typeof(int), false)]
    [DataRow(typeof(object), typeof(string), false)]
    public void IsConvertible_SupportedAndUnsupportedPairs_ReturnExpectedClassification(
        Type sourceType,
        Type targetType,
        bool expected)
    {
        bool actual = ConvertHelpers.IsConvertible(sourceType, targetType);

        Assert.AreEqual(expected, actual);
    }
}
