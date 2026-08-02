using Nebulae.Runtime.Emit.Inline;

internal static class DuplicateLabel
{
    public static void Rewrite()
    {
        IL.Label("duplicate");
        IL.Label("duplicate");
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
