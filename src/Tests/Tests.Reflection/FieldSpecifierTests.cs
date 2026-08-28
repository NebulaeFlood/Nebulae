using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
[DoNotParallelize]
public sealed class FieldSpecifierTests
{
    [TestMethod]
    public void GetAndSet_OpenReferenceTarget_ReadAndModifyProvidedInstance()
    {
        FieldInfo field = GetInstanceField();
        Reflector<ReflectionTarget>.Get<int> getter = field
            .Specify()
            .Get()
            .Compile<Reflector<ReflectionTarget>.Get<int>>();
        Reflector<ReflectionTarget>.Set<int> setter = field
            .Specify()
            .Set()
            .Compile<Reflector<ReflectionTarget>.Set<int>>();
        var target = new ReflectionTarget(1);

        setter(target, 42);

        Assert.AreEqual(42, getter(target));
        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void GetAndSet_BoundReferenceTarget_ReadAndModifyConfiguredInstance()
    {
        FieldInfo field = GetInstanceField();
        var target = new ReflectionTarget(1);
        Reflector<ReflectionTarget>.Close.Get<int> getter = field
            .Specify().Bind(target).Get()
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = field
            .Specify().Bind(target).Set()
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();

        setter(42);

        Assert.AreEqual(42, getter());
        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void Ref_OpenAndBoundReferenceTargets_ReturnAliasesToFieldStorage()
    {
        FieldInfo field = GetInstanceField();
        var openTarget = new ReflectionTarget(1);
        var boundTarget = new ReflectionTarget(2);
        Reflector<ReflectionTarget>.Ref<int> open = field
            .Specify().Ref()
            .Compile<Reflector<ReflectionTarget>.Ref<int>>();
        Reflector<ReflectionTarget>.Close.Ref<int> bound = field
            .Specify().Bind(boundTarget).Ref()
            .Compile<Reflector<ReflectionTarget>.Close.Ref<int>>();

        open(openTarget) = 41;
        bound() = 42;

        Assert.AreEqual(41, openTarget.GetFieldValue());
        Assert.AreEqual(42, boundTarget.GetFieldValue());
    }

    [TestMethod]
    public void Set_ByValueStructTargetModifiesCopy_ByRefTargetModifiesCaller()
    {
        FieldInfo field = typeof(ValueFieldTarget).Field(nameof(ValueFieldTarget.Value))!;
        Reflector<ValueFieldTarget>.Set<int> byValue = field
            .Specify().Set()
            .Compile<Reflector<ValueFieldTarget>.Set<int>>();
        Reflector<ValueFieldTarget>.ByRef.Set<int> byRef = field
            .Specify().Set()
            .Compile<Reflector<ValueFieldTarget>.ByRef.Set<int>>();
        var target = new ValueFieldTarget { Value = 1 };

        byValue(target, 20);
        Assert.AreEqual(1, target.Value);

        byRef(in target, 42);
        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void Ref_ByRefStructTarget_ReturnsAliasToCallerField()
    {
        FieldInfo field = typeof(ValueFieldTarget).Field(nameof(ValueFieldTarget.Value))!;
        Reflector<ValueFieldTarget>.ByRef.Ref<int> getter = field
            .Specify().Ref()
            .Compile<Reflector<ValueFieldTarget>.ByRef.Ref<int>>();
        var target = new ValueFieldTarget { Value = 1 };

        getter(in target) = 42;

        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void StaticField_ClosedAccessUsesNoTarget_OpenAccessIgnoresPlaceholder()
    {
        FieldInfo field = typeof(ReflectionTarget).Field("_staticFieldValue")!;
        Reflector<ReflectionTarget>.Close.Get<int> getter = field
            .Specify().Get()
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = field
            .Specify().Set()
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();
        Func<Guid, int> openGetter = field
            .Specify().Open().Get()
            .Compile<Func<Guid, int>>();
        int original = getter();

        try
        {
            setter(42);

            Assert.AreEqual(42, getter());
            Assert.AreEqual(42, openGetter(default));
            Assert.AreEqual(42, ReflectionTarget.GetStaticFieldValue());
        }
        finally
        {
            setter(original);
        }
    }

    [TestMethod]
    public void GetAndSet_LenientPolicy_ConvertsFieldValue()
    {
        FieldInfo field = GetInstanceField();
        var target = new ReflectionTarget(1);
        Reflector<ReflectionTarget>.Close.Get<string> getter = field
            .Specify().Bind(target).Get().Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Reflector<ReflectionTarget>.Close.Get<string>>();
        Reflector<ReflectionTarget>.Close.Set<string> setter = field
            .Specify().Bind(target).Set().Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Reflector<ReflectionTarget>.Close.Set<string>>();

        setter("42");

        Assert.AreEqual("42", getter());
        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void Compile_ModeNoneAndMismatchedRefReturnAreRejected()
    {
        FieldInfo field = GetInstanceField();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => field.Specify().Compile<Reflector<ReflectionTarget>.Get<int>>());
        Assert.ThrowsExactly<InvalidOperationException>(
            () => field.Specify().Ref().Lenient().Compile<Reflector<ReflectionTarget>.Get<int>>());
    }

    [TestMethod]
    public void Resolve_DeferredTarget_AccessesEachProvidedInstance()
    {
        FieldInfo field = GetInstanceField();
        FieldSpecifier deferred = field.Specify().Bind(Specifier.Defer).Get();

        Assert.ThrowsExactly<InvalidOperationException>(
            () => deferred.Compile<Reflector<ReflectionTarget>.Close.Get<int>>());

        FieldSpecifier.Compiler<Reflector<ReflectionTarget>.Close.Get<int>> compiler = deferred
            .Resolve<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Get<int> first = compiler.Compile(new ReflectionTarget(10));
        Reflector<ReflectionTarget>.Close.Get<int> second = compiler.Compile(new ReflectionTarget(20));

        Assert.AreEqual(10, first());
        Assert.AreEqual(20, second());
        Assert.ThrowsExactly<InvalidOperationException>(() => compiler.Compile());
    }

    [TestMethod]
    public void Configuration_IsImmutableAndParticipatesInEquality()
    {
        FieldInfo field = GetInstanceField();
        var target = new ReflectionTarget(1);
        FieldSpecifier original = field.Specify();
        FieldSpecifier getter = original.Get();
        FieldSpecifier getterCopy = getter.Get();
        FieldSpecifier bound = getter.Bind(target);
        FieldSpecifier lenient = bound.Lenient(SpecifierCulture.InvariantCulture);

        Assert.AreEqual(FieldSpecifierMode.None, original.Mode);
        Assert.AreEqual(getter, getterCopy);
        Assert.IsTrue(getter == getterCopy);
        Assert.AreNotEqual(original, getter);
        Assert.AreNotEqual(getter, bound);
        Assert.AreNotEqual(bound, lenient);
        Assert.AreEqual(SpecifierBinding.Close, bound.Binding);
        Assert.AreEqual(SpecifierPolicy.Lenient, lenient.Policy);
        Assert.AreEqual(getter.GetHashCode(), getterCopy.GetHashCode());
    }

    private static FieldInfo GetInstanceField() => typeof(ReflectionTarget).Field("_fieldValue")!;

    private struct ValueFieldTarget
    {
        public int Value;
    }
}
