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

        Assert.AreEqual(12, offset(new ReflectionTarget(10), 2));
        Assert.AreEqual(23, offset(new ReflectionTarget(20), 3));
    }

    [TestMethod]
    public void Compile_BoundPrivateInstanceMethod_UsesConfiguredTarget()
    {
        var target = new ReflectionTarget(10);
        MethodInfo method = typeof(ReflectionTarget).Method("Describe", typeof(int))!;
        Func<int, string> describe = method.Specify().Bind(target).Compile<Func<int, string>>();

        Assert.AreEqual("public:number:5", describe(5));
    }

    [TestMethod]
    public void Compile_OpenVirtualMethod_DispatchesToRuntimeOverride()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.VirtualOffset), typeof(int))!;
        Func<ReflectionTarget, int, int> offset = method.Specify().Compile<Func<ReflectionTarget, int, int>>();

        Assert.AreEqual(73, offset(new DerivedReflectionTarget(7), 3));
    }

    [TestMethod]
    public void Compile_StaticMethodUsesClosedSignature_OpenStaticIgnoresPlaceholder()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Multiply),
            typeof(int),
            typeof(int))!;
        Func<int, int, int> closed = method.Specify().Compile<Func<int, int, int>>();
        Func<Guid, int, int, int> open = method.Specify().Open().Compile<Func<Guid, int, int, int>>();

        Assert.AreEqual(42, closed(6, 7));
        Assert.AreEqual(42, open(Guid.NewGuid(), 6, 7));
    }

    [TestMethod]
    public void Compile_StrictOpenValueTypeMethodRequiresByRefTarget()
    {
        MethodInfo method = typeof(ValueMethodTarget).Method(nameof(ValueMethodTarget.ReadValue))!;
        var target = new ValueMethodTarget { Value = 42 };
        Reflector<ValueMethodTarget>.ByRef.Get<int> byRef = method
            .Specify()
            .Compile<Reflector<ValueMethodTarget>.ByRef.Get<int>>();

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => method.Specify().Compile<Reflector<ValueMethodTarget>.Get<int>>());

        Assert.AreEqual(42, byRef(in target));
        Assert.IsInstanceOfType<ArgumentException>(exception.InnerException);
    }

    [TestMethod]
    public void Compile_LenientOpenValueTypeMethodAcceptsByValueTarget()
    {
        MethodInfo method = typeof(ValueMethodTarget).Method(nameof(ValueMethodTarget.ReadValue))!;
        Reflector<ValueMethodTarget>.Get<int> getter = method
            .Specify()
            .Lenient()
            .Compile<Reflector<ValueMethodTarget>.Get<int>>();

        Assert.AreEqual(42, getter(new ValueMethodTarget { Value = 42 }));
    }

    [TestMethod]
    public void Compile_StrictByRefParameter_ForwardsCallerStorage()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Increment), typeof(int).MakeByRefType())!;
        RefIntAction increment = method.Specify().Compile<RefIntAction>();
        int value = 41;

        increment(ref value);

        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void Compile_ByRefReturn_ReturnsAliasToMemberStorage()
    {
        MethodInfo method = typeof(RefReturnTarget).Method(nameof(RefReturnTarget.GetValueReference))!;
        OpenRefGetter getter = method.Specify().Compile<OpenRefGetter>();
        var target = new RefReturnTarget(1);

        ref int value = ref getter(target);
        value = 42;

        Assert.AreEqual(42, target.Value);
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
    public void Compile_LenientReferenceAdaptation_FailsAtInvocationForWrongRuntimeValue()
    {
        var target = new ReflectionTarget(0);
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.TextLength), typeof(string))!;
        Func<object, int> textLength = method
            .Specify()
            .Bind(target)
            .Lenient()
            .Compile<Func<object, int>>();

        Assert.AreEqual(7, textLength("Nebulae"));
        Assert.ThrowsExactly<InvalidCastException>(() => textLength(new object()));
    }

    [TestMethod]
    public void Compile_DeferredTargetIsInvalidUntilResolvedWithActualTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        MethodSpecifier deferred = method.Specify().Bind(Specifier.Defer);

        Assert.ThrowsExactly<InvalidOperationException>(() => deferred.Compile<Func<int, int>>());

        MethodSpecifier.Compiler<Func<int, int>> compiler = deferred.Resolve<Func<int, int>>();
        Func<int, int> first = compiler.Compile(new ReflectionTarget(10));
        Func<int, int> second = compiler.Compile(new ReflectionTarget(20));

        Assert.AreEqual(11, first(1));
        Assert.AreEqual(22, second(2));
        Assert.ThrowsExactly<InvalidOperationException>(() => compiler.Compile());
    }

    [TestMethod]
    public void Compile_InvalidBoundTargetsAreRejected()
    {
        MethodInfo instance = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        MethodInfo @static = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Multiply),
            typeof(int),
            typeof(int))!;

        Assert.ThrowsExactly<InvalidOperationException>(
            () => instance.Specify().Bind(null).Compile<Func<int, int>>());
        Assert.ThrowsExactly<InvalidOperationException>(
            () => instance.Specify().Bind(new object()).Compile<Func<int, int>>());
        Assert.ThrowsExactly<InvalidOperationException>(
            () => @static.Specify().Bind(new object()).Compile<Func<int, int, int>>());
    }

    [TestMethod]
    public void Compiler_OpenSpecifierRejectsExplicitTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        MethodSpecifier.Compiler<Func<ReflectionTarget, int, int>> compiler = method
            .Specify()
            .Resolve<Func<ReflectionTarget, int, int>>();

        Assert.ThrowsExactly<InvalidOperationException>(() => compiler.Compile(new ReflectionTarget(1)));
    }

    [TestMethod]
    public void Configuration_IsImmutableAndParticipatesInEquality()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        var target = new ReflectionTarget(1);
        MethodSpecifier open = method.Specify();
        MethodSpecifier openCopy = open.Open();
        MethodSpecifier bound = open.Bind(target);
        MethodSpecifier lenient = bound.Lenient(SpecifierCulture.InvariantCulture);

        Assert.AreEqual(open, openCopy);
        Assert.IsTrue(open == openCopy);
        Assert.AreNotEqual(open, bound);
        Assert.AreNotEqual(bound, lenient);
        Assert.AreEqual(SpecifierBinding.Close, bound.Binding);
        Assert.AreEqual(SpecifierPolicy.Lenient, lenient.Policy);
        Assert.AreEqual(SpecifierCulture.InvariantCulture, lenient.Culture);
        Assert.AreEqual(open.GetHashCode(), openCopy.GetHashCode());
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

        Assert.AreEqual(5, getLength(in values));
    }
#endif

    private delegate void RefIntAction(ref int value);

    private delegate ref int OpenRefGetter(RefReturnTarget target);

    private struct ValueMethodTarget
    {
        public int Value;

        public readonly int ReadValue() => Value;
    }

    private sealed class RefReturnTarget(int value)
    {
        public int Value = value;

        public ref int GetValueReference() => ref Value;
    }
}
