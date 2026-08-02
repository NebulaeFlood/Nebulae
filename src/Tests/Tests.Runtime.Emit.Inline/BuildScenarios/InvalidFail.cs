using System;
using Nebulae.Runtime.Emit.Inline;

internal static class InvalidFail
{
    public static InvalidProgramException Rewrite()
    {
        InvalidProgramException exception = IL.Fail();
        return exception;
    }
}
