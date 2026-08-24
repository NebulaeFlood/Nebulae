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

        Func<int, string, ReflectionTarget> factory = constructor.Specify().Compile<Func<int, string, ReflectionTarget>>();
        ReflectionTarget target = factory(42, "private");

        Assert.AreEqual(42, target.Value);
        Assert.AreEqual("private", target.Label);
    }

    [TestMethod]
    public void Compile_LenientStringArgument_ConvertsAndInitializesObject()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;

        Func<string, ReflectionTarget> factory = constructor
            .Specify()
            .Lenient(SpecifierCulture.InvariantCulture)
            .Compile<Func<string, ReflectionTarget>>();
        ReflectionTarget target = factory("42");

        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void Resolve_CompileRepeatedly_CreatesIndependentObjects()
    {
        var constructor = typeof(ReflectionTarget).Constructor(typeof(int))!;
        ConstructorSpecifier.Compiler<Func<int, ReflectionTarget>> compiler = constructor
            .Specify()
            .Resolve<Func<int, ReflectionTarget>>();

        Func<int, ReflectionTarget> firstFactory = compiler.Compile();
        Func<int, ReflectionTarget> secondFactory = compiler.Compile();
        ReflectionTarget first = firstFactory(1);
        ReflectionTarget second = secondFactory(2);

        Assert.AreNotSame(first, second);
        Assert.AreEqual(1, first.Value);
        Assert.AreEqual(2, second.Value);
    }
}
