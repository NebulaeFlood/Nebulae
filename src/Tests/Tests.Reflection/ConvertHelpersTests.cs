using Nebulae.Reflection;
using System.Globalization;

namespace Tests.Reflection;

[TestClass]
public sealed class ConvertHelpersTests
{
    [TestMethod]
    public void ChangeType_NullReference_ReturnsNull()
    {
        string? result = ConvertHelpers.ChangeType<string?, string>(null, CultureInfo.InvariantCulture);

        Assert.IsNull(result);
    }

    [TestMethod]
    public void ChangeType_NullToValueType_ThrowsInvalidCastException()
    {
        Assert.ThrowsExactly<InvalidCastException>(
            () => ConvertHelpers.ChangeType<string?, int>(null, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public void ChangeType_ValueAlreadyHasTargetType_ReturnsSameReference()
    {
        var value = new Payload();

        Payload? result = ConvertHelpers.ChangeType<Payload, Payload>(value, provider: null);

        Assert.AreSame(value, result);
    }

    [TestMethod]
    public void ChangeType_PrimitiveTargetsUseIConvertibleMethods()
    {
        IFormatProvider provider = CultureInfo.InvariantCulture;

        Assert.IsTrue(ConvertHelpers.ChangeType<string, bool>("true", provider));
        Assert.AreEqual('A', ConvertHelpers.ChangeType<int, char>(65, provider));
        Assert.AreEqual(42, ConvertHelpers.ChangeType<string, int>("42", provider));
        Assert.AreEqual(42.5d, ConvertHelpers.ChangeType<string, double>("42.5", provider));
        Assert.AreEqual("42", ConvertHelpers.ChangeType<int, string>(42, provider));
        Assert.AreEqual(
            new DateTime(2026, 8, 28),
            ConvertHelpers.ChangeType<string, DateTime>("2026-08-28", provider));
    }

    [TestMethod]
    public void ChangeType_CultureSensitiveDecimal_UsesProvidedCulture()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");

        decimal value = ConvertHelpers.ChangeType<string, decimal>("12,5", culture);

        Assert.AreEqual(12.5m, value);
    }

    [TestMethod]
    public void ChangeType_CustomTargetUsesToTypeAndForwardsProvider()
    {
        var provider = CultureInfo.GetCultureInfo("en-US");
        var source = new ConvertibleProbe();

        Uri? result = ConvertHelpers.ChangeType<ConvertibleProbe, Uri>(source, provider);

        Assert.AreEqual(new Uri("https://nebulae.example/"), result);
        Assert.AreSame(provider, source.Provider);
        Assert.AreEqual(typeof(Uri), source.TargetType);
    }

    [TestMethod]
    public void ChangeType_NonConvertibleValue_ThrowsInvalidCastException()
    {
        Assert.ThrowsExactly<InvalidCastException>(
            () => ConvertHelpers.ChangeType<Payload, string>(new Payload(), CultureInfo.InvariantCulture));
    }

    [TestMethod]
    [DataRow(TypeCode.Int32, TypeCode.String, true)]
    [DataRow(TypeCode.Boolean, TypeCode.Decimal, true)]
    [DataRow(TypeCode.Char, TypeCode.Int32, true)]
    [DataRow(TypeCode.Char, TypeCode.Boolean, false)]
    [DataRow(TypeCode.DateTime, TypeCode.String, true)]
    [DataRow(TypeCode.String, TypeCode.DateTime, true)]
    [DataRow(TypeCode.DateTime, TypeCode.Int32, false)]
    [DataRow(TypeCode.Object, TypeCode.String, false)]
    [DataRow(TypeCode.Object, TypeCode.Object, false)]
    public void IsConvertible_TypeCodeMatrix_ReturnsDocumentedClassification(
        TypeCode source,
        TypeCode target,
        bool expected)
    {
        Assert.AreEqual(expected, ConvertHelpers.IsConvertible(source, target));
    }

    [TestMethod]
    public void IsConvertible_TypeOverloadMatchesTypeCodeClassificationAndHandlesNull()
    {
        Assert.IsTrue(ConvertHelpers.IsConvertible(typeof(int), typeof(string)));
        Assert.IsTrue(ConvertHelpers.IsConvertible(typeof(DateTime), typeof(string)));
        Assert.IsFalse(ConvertHelpers.IsConvertible(typeof(DateTime), typeof(int)));
        Assert.IsFalse(ConvertHelpers.IsConvertible(null!, typeof(string)));
        Assert.IsFalse(ConvertHelpers.IsConvertible(typeof(string), null!));
    }

    private sealed class Payload;

    private sealed class ConvertibleProbe : IConvertible
    {
        public IFormatProvider? Provider { get; private set; }

        public Type? TargetType { get; private set; }

        public TypeCode GetTypeCode() => TypeCode.Object;

        public object ToType(Type conversionType, IFormatProvider? provider)
        {
            Provider = provider;
            TargetType = conversionType;
            return new Uri("https://nebulae.example/");
        }

        bool IConvertible.ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();
        byte IConvertible.ToByte(IFormatProvider? provider) => throw new NotSupportedException();
        char IConvertible.ToChar(IFormatProvider? provider) => throw new NotSupportedException();
        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();
        double IConvertible.ToDouble(IFormatProvider? provider) => throw new NotSupportedException();
        short IConvertible.ToInt16(IFormatProvider? provider) => throw new NotSupportedException();
        int IConvertible.ToInt32(IFormatProvider? provider) => throw new NotSupportedException();
        long IConvertible.ToInt64(IFormatProvider? provider) => throw new NotSupportedException();
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => throw new NotSupportedException();
        float IConvertible.ToSingle(IFormatProvider? provider) => throw new NotSupportedException();
        string IConvertible.ToString(IFormatProvider? provider) => throw new NotSupportedException();
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();
        uint IConvertible.ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();
    }
}
