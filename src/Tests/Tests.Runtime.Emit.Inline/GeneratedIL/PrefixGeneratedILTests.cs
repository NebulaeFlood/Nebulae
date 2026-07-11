using Mono.Cecil.Cil;
using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class PrefixGeneratedILTests
{
    [TestMethod]
    public void PrefixIsImmediatelyFollowedBySupportedOperation()
    {
        IReadOnlyList<Instruction> instructions = CecilAssertHelpers.GetInstructions(
            typeof(PrefixGeneratedILTests).FullName!,
            nameof(ReadVolatileValue));

        int prefixIndex = -1;
        for (int index = 0; index < instructions.Count; index++)
        {
            if (instructions[index].OpCode.Code == Code.Volatile)
            {
                prefixIndex = index;
                break;
            }
        }

        Assert.AreNotEqual(-1, prefixIndex, "Expected a volatile prefix in the rewritten method.");
        Assert.AreNotEqual(instructions.Count, prefixIndex + 1, "The prefix must be followed by an instruction.");
        Assert.AreEqual(Code.Ldsfld, instructions[prefixIndex + 1].OpCode.Code);
    }

    private static int ReadVolatileValue()
    {
        IL.Emit.Volatile();
        IL.Emit.Ldsfld(IL.Ref(typeof(PrefixTarget)).Field(nameof(PrefixTarget.Value)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static class PrefixTarget
    {
        public static int Value = 131;
    }
}
