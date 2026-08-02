using Mono.Cecil;
using Mono.Cecil.Cil;
using Tests.Runtime.Emit.Inline.Behavior;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.GeneratedIL;

[TestClass]
public sealed class GenericGeneratedILTests
{
    [TestMethod]
    public void GenericMethodCall_UsesConstructedMethodReference()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(GenericBehaviorTests).FullName!,
            "CallGenericIdentity",
            static instructions =>
            {
                Instruction[] calls = [.. instructions
                    .Where(static instruction => instruction.OpCode.Code == Code.Call)];

                Assert.HasCount(1, calls);
                var method = (GenericInstanceMethod)calls[0].Operand;

                Assert.AreEqual("Identity", method.Name);
                Assert.AreEqual("Tests.Runtime.Emit.Inline.Behavior.GenericBehaviorTests/GenericMethodTarget", method.DeclaringType.FullName);
                Assert.HasCount(1, method.GenericArguments);
                Assert.AreEqual(typeof(int).FullName, method.GenericArguments[0].FullName);
                Assert.HasCount(1, method.Parameters);
            });
    }

    [TestMethod]
    public void ConstructedGenericMemberCall_UsesConstructedDeclaringType()
    {
        CecilAssertHelpers.InspectInstructions(
            typeof(GenericBehaviorTests).FullName!,
            "CallConstructedGenericMember",
            static instructions =>
            {
                Instruction[] calls = [.. instructions
                    .Where(static instruction => instruction.OpCode.Code == Code.Call)];

                Assert.HasCount(1, calls);
                var method = (MethodReference)calls[0].Operand;
                var declaringType = (GenericInstanceType)method.DeclaringType;

                Assert.AreEqual("Echo", method.Name);
                Assert.AreEqual("Tests.Runtime.Emit.Inline.Behavior.GenericBehaviorTests/GenericBox`1", declaringType.ElementType.FullName);
                Assert.HasCount(1, declaringType.GenericArguments);
                Assert.AreEqual(typeof(int).FullName, declaringType.GenericArguments[0].FullName);
                Assert.HasCount(1, method.Parameters);
            });
    }
}
