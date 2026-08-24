using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class TypeExtensionsTests
{
    [TestMethod]
    public void LookupHelpers_PrivateMembers_ReturnRequestedMembers()
    {
        Type targetType = typeof(ReflectionTarget);

        ConstructorInfo? constructor = targetType.Constructor(typeof(int), typeof(string));
        MethodInfo? method = targetType.Method("Describe", typeof(int));
        FieldInfo? field = targetType.Field("_fieldValue");
        EventInfo? eventInfo = targetType.Event("HiddenChanged");

        Assert.IsNotNull(constructor);
        Assert.AreEqual(typeof(int), constructor.GetParameters()[0].ParameterType);
        Assert.AreEqual(typeof(string), constructor.GetParameters()[1].ParameterType);
        Assert.IsNotNull(method);
        Assert.AreEqual(typeof(int), method.GetParameters()[0].ParameterType);
        Assert.IsNotNull(field);
        Assert.AreEqual(typeof(int), field.FieldType);
        Assert.IsNotNull(eventInfo);
        Assert.AreEqual(typeof(EventHandler), eventInfo.EventHandlerType);
    }

    [TestMethod]
    public void Indexer_ReturnAndParameterFilters_SelectMatchingIndexer()
    {
        Type targetType = typeof(ReflectionTarget);

        PropertyInfo? integerIndexer = targetType.Indexer(typeof(string), typeof(int));
        PropertyInfo? stringIndexer = targetType.Indexer(typeof(int), typeof(string));

        Assert.IsNotNull(integerIndexer);
        Assert.AreEqual(typeof(string), integerIndexer.PropertyType);
        Assert.AreEqual(typeof(int), integerIndexer.GetIndexParameters()[0].ParameterType);
        Assert.IsNotNull(stringIndexer);
        Assert.AreEqual(typeof(int), stringIndexer.PropertyType);
        Assert.AreEqual(typeof(string), stringIndexer.GetIndexParameters()[0].ParameterType);
    }

    [TestMethod]
    public void IsCompatible_ExactAndAssignableReferenceTypes_ReturnExpectedClassification()
    {
        Assert.IsTrue(Reflector.IsCompatible(typeof(int), typeof(int)));
        Assert.IsTrue(Reflector.IsCompatible(typeof(CompatibilityBase), typeof(CompatibilityDerived)));
        Assert.IsFalse(Reflector.IsCompatible(typeof(CompatibilityDerived), typeof(CompatibilityBase)));
        Assert.IsFalse(Reflector.IsCompatible(typeof(object), typeof(int)));
    }

    [TestMethod]
    public void IsNullable_NullableAndNonNullableTypes_ReturnExpectedClassification()
    {
        Assert.IsTrue(typeof(int?).IsNullable());
        Assert.IsFalse(typeof(int).IsNullable());
        Assert.IsFalse(typeof(string).IsNullable());
    }

    [TestMethod]
    public void IsStatic_InstanceAndStaticMembers_ReturnExpectedClassification()
    {
        const BindingFlags lookup = Reflector.DefaultLookup;
        Type targetType = typeof(ReflectionTarget);
        PropertyInfo instanceProperty = targetType.GetProperty(nameof(ReflectionTarget.Value), lookup)!;
        PropertyInfo staticProperty = targetType.GetProperty(nameof(ReflectionTarget.StaticValue), lookup)!;
        EventInfo instanceEvent = targetType.GetEvent(nameof(ReflectionTarget.Changed), lookup)!;
        EventInfo staticEvent = targetType.GetEvent(nameof(ReflectionTarget.StaticChanged), lookup)!;

        Assert.IsFalse(instanceProperty.IsStatic());
        Assert.IsTrue(staticProperty.IsStatic());
        Assert.IsFalse(instanceEvent.IsStatic());
        Assert.IsTrue(staticEvent.IsStatic());
    }

    private class CompatibilityBase;

    private sealed class CompatibilityDerived : CompatibilityBase;
}
