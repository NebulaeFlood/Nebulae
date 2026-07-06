using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class BasicBehaviorTests
{
    [TestMethod]
    public void IntegerConstantsAreReturned()
    {
        Assert.AreEqual(42, ReturnInt32Constant());
        Assert.AreEqual(9_876_543_210L, ReturnInt64Constant());
    }

    [TestMethod]
    public void FloatingPointConstantsAreReturned()
    {
        Assert.AreEqual(3.25F, ReturnSingleConstant());
        Assert.AreEqual(-12.5D, ReturnDoubleConstant());
    }

    [TestMethod]
    public void ReferenceValuesAreReturned()
    {
        Assert.AreEqual("inline IL", ReturnStringConstant());
        Assert.IsNull(ReturnNull());
    }

    [TestMethod]
    public void ValuesSurviveIntermediateStackWorkAndParameterPassing()
    {
        Assert.AreEqual(73, PreserveValueAcrossStackOperations());
        Assert.AreEqual(123, ReturnArgument(123));
    }

    private static int ReturnInt32Constant()
    {
        IL.Emit.Ldc_I4(42);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static long ReturnInt64Constant()
    {
        IL.Emit.Ldc_I8(9_876_543_210L);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static float ReturnSingleConstant()
    {
        IL.Emit.Ldc_R4(3.25F);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static double ReturnDoubleConstant()
    {
        IL.Emit.Ldc_R8(-12.5D);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static string ReturnStringConstant()
    {
        IL.Emit.Ldstr("inline IL");
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static object? ReturnNull()
    {
        IL.Emit.Ldnull();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int PreserveValueAcrossStackOperations()
    {
        IL.Emit.Ldc_I4(73);
        IL.Emit.Dup();
        IL.Emit.Pop();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnArgument(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
