using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class ExceptionBehaviorTests
{
    [TestMethod]
    public void ExpectedExceptionsAreHandled()
    {
        Assert.AreEqual(109, CatchExpectedException(new InvalidOperationException()));
    }

    [TestMethod]
    public void FinallyBlocksAlwaysRun()
    {
        int finallyCount = 0;

        int result = SetValueAndRunFinally(() => finallyCount++);

        Assert.AreEqual(127, result);
        Assert.AreEqual(1, finallyCount);
    }

    private static int CatchExpectedException(Exception exception)
    {
        int result = 0;

        try
        {
            IL.Emit.Ldarg(exception);
            IL.Emit.Throw();
        }
        catch (InvalidOperationException)
        {
            result = 109;
        }

        return result;
    }

    private static int SetValueAndRunFinally(Action onFinally)
    {
        int result = 0;

        try
        {
            IL.Emit.Ldc_I4(127);
            IL.Emit.Stloc(result);
        }
        finally
        {
            onFinally();
        }

        return result;
    }
}
