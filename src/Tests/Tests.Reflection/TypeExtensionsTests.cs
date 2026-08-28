using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class TypeExtensionsTests
{
    [TestMethod]
    public void Indexer_MissingDerivedOverloadFallsBackToBaseType()
    {
        PropertyInfo? declared = typeof(IndexerDerived).Indexer(typeof(Guid));
        PropertyInfo? inherited = typeof(IndexerDerived).Indexer(typeof(int));
        PropertyInfo? inheritedPrivate = typeof(IndexerDerived).Indexer(
            BindingFlags.NonPublic | BindingFlags.Instance,
            typeof(string));

        Assert.AreEqual(typeof(IndexerDerived), declared?.DeclaringType);
        Assert.AreEqual(typeof(IndexerBase), inherited?.DeclaringType);
        Assert.AreEqual(typeof(IndexerBase), inheritedPrivate?.DeclaringType);
    }

    [TestMethod]
    public void Indexer_DeclaredOnlyPreventsBaseTypeFallback()
    {
        const BindingFlags declaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        Assert.IsNull(typeof(IndexerDerived).Indexer(declaredPublic, typeof(int)));
        Assert.AreEqual(
            typeof(IndexerDerived),
            typeof(IndexerDerived).Indexer(declaredPublic, typeof(Guid))?.DeclaringType);
    }

    [TestMethod]
    public void IsStatic_UsesNonPublicAndSingleAvailableAccessors()
    {
        Type targetType = typeof(ReflectionTarget);
        PropertyInfo instanceProperty = targetType.Property("HiddenValue")!;
        PropertyInfo staticWriteOnly = targetType.Property(nameof(ReflectionTarget.StaticWriteOnlyValue))!;
        EventInfo instanceEvent = targetType.Event("HiddenChanged")!;
        EventInfo staticEvent = targetType.Event(nameof(ReflectionTarget.StaticChanged))!;

        Assert.IsFalse(instanceProperty.IsStatic());
        Assert.IsTrue(staticWriteOnly.IsStatic());
        Assert.IsFalse(instanceEvent.IsStatic());
        Assert.IsTrue(staticEvent.IsStatic());
    }

    [TestMethod]
    public void IsCompatible_ExactValueAndAssignableReferenceTypesAreCompatible()
    {
        Assert.IsTrue(Reflector.IsCompatible(typeof(int), typeof(int)));
        Assert.IsTrue(Reflector.IsCompatible(typeof(CompatibilityBase), typeof(CompatibilityDerived)));
        Assert.IsTrue(Reflector.IsCompatible(typeof(IDisposable), typeof(MemoryStream)));
    }

    [TestMethod]
    public void IsCompatible_ReverseReferenceAndNonExactValueTypesAreNotCompatible()
    {
        Assert.IsFalse(Reflector.IsCompatible(typeof(CompatibilityDerived), typeof(CompatibilityBase)));
        Assert.IsFalse(Reflector.IsCompatible(typeof(long), typeof(int)));
        Assert.IsFalse(Reflector.IsCompatible(typeof(object), typeof(int)));
        Assert.IsFalse(Reflector.IsCompatible(typeof(int), typeof(object)));
    }

    [TestMethod]
    public void IsNullable_RecognizesOpenAndConstructedNullableOnly()
    {
        Assert.IsTrue(typeof(Nullable<>).IsNullable());
        Assert.IsTrue(typeof(int?).IsNullable());
        Assert.IsFalse(typeof(int).IsNullable());
        Assert.IsFalse(typeof(string).IsNullable());
    }


    private class CompatibilityBase;

    private sealed class CompatibilityDerived : CompatibilityBase;
}
