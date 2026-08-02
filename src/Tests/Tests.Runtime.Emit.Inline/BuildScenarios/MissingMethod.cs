using Nebulae.Runtime.Emit.Inline;

internal static class MissingMethod
{
    public static void Rewrite()
    {
        IL.Emit.Call(IL.Ref(typeof(string)).Method("Missing"));
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
