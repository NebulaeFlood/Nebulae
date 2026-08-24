using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class MethodSpecifierTests
{
    [TestMethod]
    public void Compile_OpenInstanceMethod_UsesInvocationTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        Func<ReflectionTarget, int, int> offset = method.Specify().Compile<Func<ReflectionTarget, int, int>>();

        int first = offset(new ReflectionTarget(10), 2);
        int second = offset(new ReflectionTarget(20), 3);

        Assert.AreEqual(12, first);
        Assert.AreEqual(23, second);
    }

    [TestMethod]
    public void Compile_BoundInstanceMethod_UsesConfiguredTarget()
    {
        var target = new ReflectionTarget(10);
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;

        Func<int, int> offset = method.Specify().Bind(target).Compile<Func<int, int>>();

        Assert.AreEqual(15, offset(5));
    }

    [TestMethod]
    public void Compile_StaticMethod_InvokesClosedSignature()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Multiply),
            typeof(int),
            typeof(int))!;

        Func<int, int, int> multiply = method.Specify().Compile<Func<int, int, int>>();

        Assert.AreEqual(42, multiply(6, 7));
    }

    [TestMethod]
    public void Resolve_DeferredTarget_ProducesDelegatesForEachTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        MethodSpecifier.Compiler<Func<int, int>> compiler = method
            .Specify()
            .Bind(Specifier.Defer)
            .Resolve<Func<int, int>>();

        Func<int, int> first = compiler.Compile(new ReflectionTarget(10));
        Func<int, int> second = compiler.Compile(new ReflectionTarget(20));

        Assert.AreEqual(11, first(1));
        Assert.AreEqual(22, second(2));
    }

    [TestMethod]
    public void Compile_LenientConvertibleArgumentAndReturn_ConvertsBothDirections()
    {
        var target = new ReflectionTarget(1);
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;

        Func<string, string> offset = method
            .Specify()
            .Bind(target)
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Func<string, string>>();

        Assert.AreEqual("42", offset("41"));
    }

    [TestMethod]
    public void Compile_LenientReferenceArgument_UsesRuntimeCompatibleValue()
    {
        var target = new ReflectionTarget(0);
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.TextLength), typeof(string))!;

        Func<object, int> textLength = method
            .Specify()
            .Bind(target)
            .Lenient()
            .Compile<Func<object, int>>();

        Assert.AreEqual(7, textLength("Nebulae"));
    }

#if NET10_0_OR_GREATER
    [TestMethod]
    public void Compile_ByRefGetterWithSpanTarget_ReturnsLength()
    {
        MethodInfo getter = typeof(Span<int>)
            .GetProperty(nameof(Span<>.Length), Reflector.DefaultLookup)!
            .GetMethod!;
        Reflector<Span<int>>.ByRef.Get<int> getLength = getter
            .Specify()
            .Compile<Reflector<Span<int>>.ByRef.Get<int>>();
        Span<int> values = stackalloc int[5];

        int length = getLength(in values);

        Assert.AreEqual(5, length);
    }
#endif
}
