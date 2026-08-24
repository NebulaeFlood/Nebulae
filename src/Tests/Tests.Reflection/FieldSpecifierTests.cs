using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class FieldSpecifierTests
{
    [TestMethod]
    public void GetAndSet_OpenSpecifier_ReadAndModifyProvidedTarget()
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
    }

    [TestMethod]
    public void GetAndSet_BoundSpecifier_ReadAndModifyConfiguredTarget()
    {
        FieldInfo field = GetInstanceField();
        var target = new ReflectionTarget(1);
        Reflector<ReflectionTarget>.Close.Get<int> getter = field
            .Specify()
            .Bind(target)
            .Get()
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = field
            .Specify()
            .Bind(target)
            .Set()
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();

        setter(42);

        Assert.AreEqual(42, getter());
    }

    [TestMethod]
    public void Ref_OpenSpecifier_ReturnsAliasToProvidedTargetField()
    {
        FieldInfo field = GetInstanceField();
        Reflector<ReflectionTarget>.Ref<int> getter = field
            .Specify()
            .Ref()
            .Compile<Reflector<ReflectionTarget>.Ref<int>>();
        var target = new ReflectionTarget(1);

        ref int value = ref getter(target);
        value = 42;

        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void Ref_BoundSpecifier_ReturnsAliasToConfiguredTargetField()
    {
        FieldInfo field = GetInstanceField();
        var target = new ReflectionTarget(1);
        Reflector<ReflectionTarget>.Close.Ref<int> getter = field
            .Specify()
            .Bind(target)
            .Ref()
            .Compile<Reflector<ReflectionTarget>.Close.Ref<int>>();

        ref int value = ref getter();
        value = 42;

        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void StaticField_ClosedGetAndSet_ReadAndModifyValue()
    {
        FieldInfo field = typeof(ReflectionTarget).Field("_staticFieldValue")!;
        Reflector<ReflectionTarget>.Close.Get<int> getter = field
            .Specify()
            .Get()
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = field
            .Specify()
            .Set()
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();
        int original = getter();

        try
        {
            setter(42);

            Assert.AreEqual(42, getter());
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
            .Specify()
            .Bind(target)
            .Get()
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Reflector<ReflectionTarget>.Close.Get<string>>();
        Reflector<ReflectionTarget>.Close.Set<string> setter = field
            .Specify()
            .Bind(target)
            .Set()
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Reflector<ReflectionTarget>.Close.Set<string>>();

        setter("42");

        Assert.AreEqual("42", getter());
        Assert.AreEqual(42, target.GetFieldValue());
    }

    [TestMethod]
    public void Resolve_DeferredTarget_AccessesEachProvidedInstance()
    {
        FieldInfo field = GetInstanceField();
        FieldSpecifier.Compiler<Reflector<ReflectionTarget>.Close.Get<int>> compiler = field
            .Specify()
            .Bind(Specifier.Defer)
            .Get()
            .Resolve<Reflector<ReflectionTarget>.Close.Get<int>>();

        Reflector<ReflectionTarget>.Close.Get<int> first = compiler.Compile(new ReflectionTarget(10));
        Reflector<ReflectionTarget>.Close.Get<int> second = compiler.Compile(new ReflectionTarget(20));

        Assert.AreEqual(10, first());
        Assert.AreEqual(20, second());
    }

    private static FieldInfo GetInstanceField() => typeof(ReflectionTarget).Field("_fieldValue")!;
}
