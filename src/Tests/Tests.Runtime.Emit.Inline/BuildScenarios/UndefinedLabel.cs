using Nebulae.Runtime.Emit.Inline;

internal static class UndefinedLabel
{
    public static int Rewrite()
    {
        IL.Emit.Br("missing");
        IL.Emit.Ldc_I4(1);
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
