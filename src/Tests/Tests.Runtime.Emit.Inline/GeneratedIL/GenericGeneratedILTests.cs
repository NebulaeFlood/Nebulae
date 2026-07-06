using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Tests.Runtime.Emit.Inline.Behavior;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class GenericGeneratedILTests
{
    [TestMethod]
    public void GenericMethodCallUsesConstructedMethodReference()
    {
        IReadOnlyList<Instruction> instructions = CecilAssertHelpers.GetInstructions(
            typeof(GenericBehaviorTests).FullName!,
            "CallGenericIdentity");

        Assert.IsTrue(instructions.Any(static instruction =>
            instruction.OpCode.Code is Code.Call or Code.Callvirt
            && instruction.Operand is GenericInstanceMethod));
    }

    [TestMethod]
    public void ConstructedGenericMemberCallUsesConstructedDeclaringType()
    {
        IReadOnlyList<Instruction> instructions = CecilAssertHelpers.GetInstructions(
            typeof(GenericBehaviorTests).FullName!,
            "CallConstructedGenericMember");

        Assert.IsTrue(instructions.Any(static instruction =>
            instruction.OpCode.Code is Code.Call or Code.Callvirt
            && instruction.Operand is MethodReference { DeclaringType: GenericInstanceType }));
    }
}
