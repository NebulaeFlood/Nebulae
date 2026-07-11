using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class RewriteCompletionTests
{
    [TestMethod]
    public void RewrittenAssemblyContainsNoPlaceholderCalls()
    {
        CecilAssertHelpers.AssertNoPlaceholderCalls();
    }

    [TestMethod]
    public void RewrittenAssemblyContainsNoPlaceholderAssemblyReference()
    {
        CecilAssertHelpers.AssertNoPlaceholderAssemblyReference();
    }

    private sealed class Sentinel
    {
        public static int Rewrite()
        {
            IL.Emit.Ldc_I4(42);
            IL.Emit.Ret();
            throw IL.Fail();
        }
    }
}
