using Nebulae.Runtime.Emit.Inline;

internal static class InvalidTail
{
    public static int Rewrite()
    {
        IL.Emit.Tail();
        IL.Emit.Ldc_I4(1);
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
