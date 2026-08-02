#if NET10_0_OR_GREATER
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class TargetFrameworkBehaviorTests
{
    [TestMethod]
    public void RefStructArgument_OnNet10_CanBeLoaded()
    {
        Span<int> values = [37, 41];

        Span<int> result = ReturnSpan(values);

        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(37, result[0]);
        Assert.AreEqual(41, result[1]);
    }

    private static Span<int> ReturnSpan(Span<int> value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
#endif
