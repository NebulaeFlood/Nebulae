using Mono.Cecil.Cil;
using Mono.Cecil;
using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class PrefixGeneratedILTests
{
    [TestMethod]
    public void Prefix_IsImmediatelyFollowedBySupportedOperation()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(PrefixGeneratedILTests).FullName!,
            nameof(ReadVolatileValue),
            static instructions =>
            {
                int[] prefixIndexes = [.. instructions
                    .Select(static (instruction, index) => (instruction, index))
                    .Where(static item => item.instruction.OpCode.Code == Code.Volatile)
                    .Select(static item => item.index)];

                Assert.HasCount(1, prefixIndexes, "Expected exactly one volatile prefix in the rewritten method.");
                int prefixIndex = prefixIndexes[0];
                Assert.IsLessThan(instructions.Count, prefixIndex + 1, "The prefix must be followed by an instruction.");

                Instruction fieldLoad = instructions[prefixIndex + 1];
                Assert.AreEqual(Code.Ldsfld, fieldLoad.OpCode.Code);
                var field = (FieldReference)fieldLoad.Operand;
                Assert.AreEqual(nameof(PrefixTarget.Value), field.Name);
                Assert.AreEqual("Tests.Runtime.Emit.Inline.GeneratedIL.PrefixGeneratedILTests/PrefixTarget", field.DeclaringType.FullName);
            });
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
