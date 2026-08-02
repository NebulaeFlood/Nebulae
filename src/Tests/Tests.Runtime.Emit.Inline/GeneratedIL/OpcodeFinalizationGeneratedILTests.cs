using Mono.Cecil;
using Mono.Cecil.Cil;
using Nebulae.Runtime.Emit.Inline;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class OpcodeFinalizationGeneratedILTests
{
    [TestMethod]
    [DataRow(nameof(ReturnMinusOne), Code.Ldc_I4_M1)]
    [DataRow(nameof(ReturnZero), Code.Ldc_I4_0)]
    [DataRow(nameof(ReturnEight), Code.Ldc_I4_8)]
    [DataRow(nameof(ReturnSByte), Code.Ldc_I4_S)]
    [DataRow(nameof(ReturnInt32), Code.Ldc_I4)]
    public void IntegerConstant_UsesSmallestEncoding(string methodName, Code expectedCode)
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            methodName,
            instructions => Assert.AreEqual(
                expectedCode,
                instructions.First(static instruction => instruction.OpCode.Code != Code.Nop).OpCode.Code));
    }

    [TestMethod]
    public void ArgumentsAndLocals_UseShortForms()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(ReturnFirstArgument),
            static instructions => Assert.AreEqual(
                Code.Ldarg_0,
                instructions.First(static instruction => instruction.OpCode.Code != Code.Nop).OpCode.Code));

        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(ReturnFifthArgument),
            static instructions => Assert.AreEqual(
                Code.Ldarg_S,
                instructions.First(static instruction => instruction.OpCode.Code != Code.Nop).OpCode.Code));

        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(ReturnLocal),
            static instructions =>
            {
                Assert.IsTrue(instructions.Any(static instruction => instruction.OpCode.Code == Code.Stloc_0));
                Assert.IsTrue(instructions.Any(static instruction => instruction.OpCode.Code == Code.Ldloc_0));
            });
    }

    [TestMethod]
    public void NearbyBranch_UsesShortEncoding()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(SelectValue),
            static instructions => Assert.IsTrue(instructions.Any(static instruction => instruction.OpCode.Code == Code.Brfalse_S)));
    }

    [TestMethod]
    public void GenericArrayInstructions_AreSpecializedForElementType()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(ReadGenericElement),
            static instructions => Assert.IsTrue(instructions.Any(static instruction => instruction.OpCode.Code == Code.Ldelem_I4)));

        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(WriteGenericElement),
            static instructions => Assert.IsTrue(instructions.Any(static instruction => instruction.OpCode.Code == Code.Stelem_I4)));
    }

    [TestMethod]
    public void TailPrefix_IsImmediatelyFollowedByExactCallAndReturn()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(OpcodeFinalizationGeneratedILTests).FullName!,
            nameof(TailCall),
            static instructions =>
            {
                int tailIndex = instructions
                    .Select(static (instruction, index) => (instruction, index))
                    .Single(static item => item.instruction.OpCode.Code == Code.Tail)
                    .index;

                Assert.IsLessThan(instructions.Count, tailIndex + 2);
                Assert.AreEqual(Code.Call, instructions[tailIndex + 1].OpCode.Code);
                var method = (MethodReference)instructions[tailIndex + 1].Operand;
                Assert.AreEqual("Increment", method.Name);
                Assert.AreEqual(Code.Ret, instructions[tailIndex + 2].OpCode.Code);
            });
    }

    private static int ReturnMinusOne()
    {
        IL.Emit.Ldc_I4(-1);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnZero()
    {
        IL.Emit.Ldc_I4(0);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnEight()
    {
        IL.Emit.Ldc_I4(8);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnSByte()
    {
        IL.Emit.Ldc_I4(42);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnInt32()
    {
        IL.Emit.Ldc_I4(1000);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnFirstArgument(int first, int second)
    {
        IL.Emit.Ldarg(first);
        IL.Emit.Ldarg(second);
        IL.Emit.Pop();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnFifthArgument(int first, int second, int third, int fourth, int fifth)
    {
        IL.Emit.Ldarg(fifth);
        IL.Emit.Ldarg(first);
        IL.Emit.Pop();
        IL.Emit.Ldarg(second);
        IL.Emit.Pop();
        IL.Emit.Ldarg(third);
        IL.Emit.Pop();
        IL.Emit.Ldarg(fourth);
        IL.Emit.Pop();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReturnLocal()
    {
        IL.Emit.Ldc_I4(67);
        IL.Emit.Stloc(out int value);
        IL.Emit.Ldloc(value);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int SelectValue(bool condition)
    {
        IL.Emit.Ldarg(condition);
        IL.Emit.Brfalse("false");
        IL.Emit.Ldc_I4(1);
        IL.Emit.Ret();
        IL.Label("false");
        IL.Emit.Ldc_I4(0);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReadGenericElement(int[] values, int index)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldelem(typeof(int));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void WriteGenericElement(int[] values, int index, int value)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldarg(value);
        IL.Emit.Stelem(typeof(int));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int TailCall(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Tail();
        IL.Emit.Call(IL.Ref(typeof(OpcodeFinalizationGeneratedILTests)).Method(nameof(Increment), typeof(int), typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int Increment(int value) => value + 1;
}
