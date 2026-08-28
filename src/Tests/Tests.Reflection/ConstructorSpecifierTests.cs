using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class ConstructorSpecifierTests
{
    [TestMethod]
    public void Compile_PrivateConstructor_ForwardsArgumentsAndInitializesObject()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int), typeof(string))!;

        Func<int, string, ReflectionTarget> factory = constructor
            .Specify()
            .Compile<Func<int, string, ReflectionTarget>>();

        ReflectionTarget target = factory(42, "private");

        Assert.AreEqual(42, target.Value);
        Assert.AreEqual("private", target.Label);
    }

    [TestMethod]
    public void Compile_StrictPolicyRejectsValueTypeConversion()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;

        InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
            () => constructor.Specify().Compile<Func<string, ReflectionTarget>>());

        Assert.IsInstanceOfType<ArgumentException>(exception.InnerException);
    }

    [TestMethod]
    public void Compile_LenientPolicyConvertsArgumentUsingSelectedCulture()
    {
        var constructor = typeof(DecimalConstructorTarget).Constructor(typeof(decimal))!;
        Func<string, DecimalConstructorTarget> factory = constructor
            .Specify()
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Func<string, DecimalConstructorTarget>>();

        DecimalConstructorTarget target = factory("12.5");

        Assert.AreEqual(12.5m, target.Value);
    }

    [TestMethod]
    public void Compile_ReferenceReturnCovariance_AllowsBaseReturnType()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        Func<int, object> factory = constructor.Specify().Compile<Func<int, object>>();

        object target = factory(7);

        Assert.IsInstanceOfType<ReflectionTarget>(target);
        Assert.AreEqual(7, ((ReflectionTarget)target).Value);
    }

    [TestMethod]
    public void Compiler_CreatesFactory()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        ConstructorSpecifier.Compiler<Func<int, ReflectionTarget>> compiler = constructor
            .Specify()
            .Resolve<Func<int, ReflectionTarget>>();
        ReflectionTarget target = compiler.Compile()(42);

        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void Configuration_IsImmutableAndParticipatesInEquality()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        ConstructorSpecifier strict = constructor.Specify();
        ConstructorSpecifier strictCopy = strict.Strict();
        ConstructorSpecifier lenient = strict.Lenient(SpecifierCulture.InvariantCulture);

        Assert.AreEqual(SpecifierPolicy.Strict, strict.Policy);
        Assert.AreEqual(strict, strictCopy);
        Assert.IsTrue(strict == strictCopy);
        Assert.AreNotEqual(strict, lenient);
        Assert.IsTrue(strict != lenient);
        Assert.AreEqual(SpecifierPolicy.Lenient, lenient.Policy);
        Assert.AreEqual(SpecifierCulture.InvariantCulture, lenient.Culture);
        Assert.AreEqual(strict.GetHashCode(), strictCopy.GetHashCode());
    }

    private sealed class DecimalConstructorTarget(decimal value)
    {
        public decimal Value { get; } = value;
    }
}
