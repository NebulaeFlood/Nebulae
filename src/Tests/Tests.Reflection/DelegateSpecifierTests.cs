using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class DelegateSpecifierTests
{
    [TestMethod]
    public void Compile_ConstructorInputs_CreatesParameterlessFactory()
    {
        ConstructorInfo constructor = typeof(ReflectionTarget).Constructor(typeof(int), typeof(string))!;
        Func<ReflectionTarget> factory = constructor
            .Specify()
            .Input(42, "captured")
            .Compile<Func<ReflectionTarget>>();

        ReflectionTarget target = factory();

        Assert.AreEqual(42, target.Value);
        Assert.AreEqual("captured", target.Label);
    }

    [TestMethod]
    public void Compile_AllConstantInputsOnBoundInstance_StillBindsTarget()
    {
        var target = new ReflectionTarget(10);
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        Func<int> offset = method
            .Specify()
            .Bind(target)
            .Input(5)
            .Compile<Func<int>>();

        Assert.AreEqual(15, offset());
    }

    [TestMethod]
    public void Compile_OpenInstanceInputs_LeavesOnlyInvocationTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        Func<ReflectionTarget, int> offset = method
            .Specify()
            .Input(5)
            .Compile<Func<ReflectionTarget, int>>();

        Assert.AreEqual(15, offset(new ReflectionTarget(10)));
        Assert.AreEqual(25, offset(new ReflectionTarget(20)));
    }

    [TestMethod]
    public void Compile_OpenStaticInputs_IgnoresPlaceholder()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Multiply),
            typeof(int),
            typeof(int))!;
        Func<Guid, int> multiply = method
            .Specify()
            .Open()
            .Input(6, 7)
            .Compile<Func<Guid, int>>();

        Assert.AreEqual(42, multiply(default));
    }

    [TestMethod]
    public void Compile_GenericEightInputs_PreservesTypesAndSupportsOpenInstanceTarget()
    {
        MethodInfo method = typeof(EightArgumentTarget).Method(
            nameof(EightArgumentTarget.Combine),
            typeof(Marker), typeof(Marker), typeof(Marker), typeof(Marker),
            typeof(Marker), typeof(Marker), typeof(Marker), typeof(Marker))!;
        Func<EightArgumentTarget, string> combine = method
            .Specify()
            .Input(
                new Marker("1"), new Marker("2"), new Marker("3"), new Marker("4"),
                new Marker("5"), new Marker("6"), new Marker("7"), new Marker("8"))
            .Compile<Func<EightArgumentTarget, string>>();

        Assert.AreEqual("1:2:3:4:5:6:7:8", combine(new EightArgumentTarget()));
    }

    [TestMethod]
    public void Compile_ParamsNineInputs_CompressesConstantsWithoutChangingOrder()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(
            nameof(CombineNine),
            typeof(int), typeof(int), typeof(int), typeof(int), typeof(Marker),
            typeof(int), typeof(int), typeof(int), typeof(int))!;
        Func<string> combine = method
            .Specify()
            .Input(1, 2, 3, 4, new Marker("middle"), 6, 7, 8, 9)
            .Compile<Func<string>>();

        Assert.AreEqual("1:2:3:4:middle:6:7:8:9", combine());
    }

    [TestMethod]
    public void Compile_ParamsMixedInputs_PreservesRemainingClosureOrder()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(
            nameof(CombineFive),
            typeof(int), typeof(Marker), typeof(int), typeof(Marker), typeof(int))!;
        object?[] input = [1, new Marker("second"), 3, new Marker("fourth"), 5];
        Func<string> combine = method.Specify().Input(input.AsSpan()).Compile<Func<string>>();

        Assert.AreEqual("1:second:3:fourth:5", combine());
    }

    [TestMethod]
    public void Compile_NullReferenceInput_IsPassedAsNullInStrictMode()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(nameof(DescribeReference), typeof(string))!;
        Func<string> describe = method.Specify().Input((object?)null).Compile<Func<string>>();

        Assert.AreEqual("null", describe());
    }

    [TestMethod]
    public void Compile_NullNullableInput_RequiresLenientPolicy()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(nameof(DescribeNullable), typeof(int?))!;
        DelegateSpecifier strict = method.Specify().Input((object?)null);
        Func<string> lenient = strict.Lenient().Compile<Func<string>>();

        Assert.ThrowsExactly<InvalidOperationException>(() => strict.Compile<Func<string>>());
        Assert.AreEqual("null", lenient());
    }

    [TestMethod]
    public void Compile_LenientCapturedInputAndReturn_ConvertsBothDirections()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(nameof(Increment), typeof(int))!;
        Func<string> increment = method
            .Specify()
            .Input("41")
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Func<string>>();

        Assert.AreEqual("42", increment());
    }

    [TestMethod]
    public void Input_EmptyInputIsRejected_CompileRejectsWrongInputCount()
    {
        ConstructorInfo constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        MethodInfo method = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Multiply),
            typeof(int),
            typeof(int))!;

        Assert.ThrowsExactly<ArgumentException>(() => constructor.Specify().Input());
        Assert.ThrowsExactly<InvalidOperationException>(
            () => method.Specify().Input(1).Compile<Func<int>>());
    }

    [TestMethod]
    public void Compile_CapturedByRefValue_PassesWritableValue()
    {
        MethodInfo method = typeof(DelegateSpecifierTests).Method(
            nameof(IncrementDecimal),
            typeof(decimal).MakeByRefType())!;
        Func<decimal> increment = method
            .Specify()
            .Input(1m)
            .Compile<Func<decimal>>();

        Assert.AreEqual(2m, increment());
    }

    [TestMethod]
    public void Resolve_StableDeferredTarget_BindsEachCompilationToExplicitTarget()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(
            nameof(ReflectionTarget.Offset),
            typeof(int))!;
        DelegateSpecifier.Compiler<Func<int>> compiler = method
            .Specify()
            .Bind(Specifier.Defer)
            .Input(2)
            .Stable()
            .Resolve<Func<int>>();
        Func<int> first = compiler.Compile(new ReflectionTarget(10));
        Func<int> second = compiler.Compile(new ReflectionTarget(20));

        Assert.AreEqual(12, first());
        Assert.AreEqual(22, second());
    }

    [TestMethod]
    public void ConstructorDelegateSpecifier_CannotOpenOrBindTarget()
    {
        ConstructorInfo constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        DelegateSpecifier specifier = constructor.Specify().Input(1);

        Assert.ThrowsExactly<InvalidOperationException>(
            () => specifier.Open().Compile<Func<ReflectionTarget>>());
        Assert.ThrowsExactly<InvalidOperationException>(
            () => specifier.Bind(new object()).Compile<Func<ReflectionTarget>>());
    }

    [TestMethod]
    public void ConfigurationAndClosureIdentity_ParticipateInEquality()
    {
        MethodInfo method = typeof(ReflectionTarget).Method(nameof(ReflectionTarget.Offset), typeof(int))!;
        DelegateSpecifier original = method.Specify().Input(1);
        DelegateSpecifier copy = original.Strict();
        DelegateSpecifier separateClosure = method.Specify().Input(1);
        DelegateSpecifier stable = original.Stable();

        Assert.AreEqual(original, copy);
        Assert.IsTrue(original.Equals((object)copy));
        Assert.IsTrue(original == copy);
        Assert.AreEqual(original.GetHashCode(), copy.GetHashCode());
        Assert.AreNotEqual(original, separateClosure);
        Assert.IsTrue(original != separateClosure);
        Assert.AreNotEqual(original, stable);
        Assert.IsTrue(original != stable);
        Assert.AreEqual(DelegateSpecifierStability.Mutable, original.Stability);
        Assert.AreEqual(DelegateSpecifierStability.Stable, stable.Stability);
    }

    private static string CombineFive(int first, Marker second, int third, Marker fourth, int fifth)
    {
        return $"{first}:{second.Value}:{third}:{fourth.Value}:{fifth}";
    }

    private static string CombineNine(
        int first,
        int second,
        int third,
        int fourth,
        Marker marker,
        int sixth,
        int seventh,
        int eighth,
        int ninth)
    {
        return $"{first}:{second}:{third}:{fourth}:{marker.Value}:{sixth}:{seventh}:{eighth}:{ninth}";
    }

    private static int Increment(int value) => value + 1;

    private static string DescribeReference(string? value) => value ?? "null";

    private static string DescribeNullable(int? value) => value?.ToString() ?? "null";

    private static decimal IncrementDecimal(ref decimal value) => ++value;

    private sealed class EightArgumentTarget
    {
        private readonly string _separator = ":";

        public string Combine(
            Marker first,
            Marker second,
            Marker third,
            Marker fourth,
            Marker fifth,
            Marker sixth,
            Marker seventh,
            Marker eighth)
        {
            return string.Join(
                _separator,
                first.Value,
                second.Value,
                third.Value,
                fourth.Value,
                fifth.Value,
                sixth.Value,
                seventh.Value,
                eighth.Value);
        }
    }

    private sealed class Marker(string value)
    {
        public string Value { get; } = value;
    }
}
