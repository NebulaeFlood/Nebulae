using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class ExtendedBehaviorTests
{
    [TestMethod]
    [DataRow(0, 100)]
    [DataRow(1, 200)]
    [DataRow(2, -1)]
    public void SwitchCasesEndingWithFail_ReturnExpectedValue(int value, int expected)
    {
        Assert.AreEqual(expected, SelectValue(value));
    }

    private static int SelectValue(int value)
    {
        switch (value)
        {
            case 0:
                IL.Emit.Ldc_I4(100);
                IL.Emit.Ret();
                throw IL.Fail();
            case 1:
                IL.Emit.Ldc_I4(200);
                IL.Emit.Ret();
                throw IL.Fail();
            default:
                IL.Emit.Ldc_I4(-1);
                IL.Emit.Ret();
                throw IL.Fail();
        }
    }
}
