using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class BranchingBehaviorTests
{
    [TestMethod]
    public void BooleanInputSelectsExpectedValue()
    {
        Assert.AreEqual(10, SelectValue(true));
        Assert.AreEqual(20, SelectValue(false));
    }

    [TestMethod]
    public void PositiveRangeIsSummed()
    {
        Assert.AreEqual(0, SumTo(0));
        Assert.AreEqual(1, SumTo(1));
        Assert.AreEqual(15, SumTo(5));
    }

    [TestMethod]
    public void KnownAndUnknownCasesAreSelected()
    {
        Assert.AreEqual(100, SelectCase(0));
        Assert.AreEqual(200, SelectCase(1));
        Assert.AreEqual(300, SelectCase(2));
        Assert.AreEqual(-1, SelectCase(3));
    }

    private static int SelectValue(bool condition)
    {
        IL.Emit.Ldarg(condition);
        IL.Emit.Brfalse("false");
        IL.Emit.Ldc_I4(10);
        IL.Emit.Ret();

        IL.Label("false");
        IL.Emit.Ldc_I4(20);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int SumTo(int value)
    {
        IL.Emit.Ldc_I4(0);
        IL.Emit.Stloc(out int sum);
        IL.Emit.Ldc_I4(1);
        IL.Emit.Stloc(out int current);

        IL.Label("loop");
        IL.Emit.Ldloc(current);
        IL.Emit.Ldarg(value);
        IL.Emit.Bgt("done");

        IL.Emit.Ldloc(sum);
        IL.Emit.Ldloc(current);
        IL.Emit.Add();
        IL.Emit.Stloc(sum);

        IL.Emit.Ldloc(current);
        IL.Emit.Ldc_I4(1);
        IL.Emit.Add();
        IL.Emit.Stloc(current);
        IL.Emit.Br("loop");

        IL.Label("done");
        IL.Emit.Ldloc(sum);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int SelectCase(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Switch("zero", "one", "two");
        IL.Emit.Ldc_I4(-1);
        IL.Emit.Ret();

        IL.Label("zero");
        IL.Emit.Ldc_I4(100);
        IL.Emit.Ret();

        IL.Label("one");
        IL.Emit.Ldc_I4(200);
        IL.Emit.Ret();

        IL.Label("two");
        IL.Emit.Ldc_I4(300);
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
