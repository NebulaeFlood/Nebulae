using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class UnsafeBehaviorTests
{
    [TestMethod]
    public void StaticFunctionPointersCanBeInvoked()
    {
        Assert.AreEqual(79, InvokeStaticFunctionPointer());
    }

    [TestMethod]
    public void VirtualFunctionPointersDispatchToOverrides()
    {
        Assert.AreEqual(83, InvokeVirtualFunctionPointer(new OverrideTarget()));
    }

    [TestMethod]
    public unsafe void FixedArrayElementsCanBeReadThroughPointers()
    {
        int[] values = [89, 97];

        fixed (int* pointer = values)
        {
            Assert.AreEqual(97, ReadPointer(pointer + 1));
        }
    }

    [TestMethod]
    public unsafe void FixedArrayElementsCanBeWrittenThroughPointers()
    {
        int[] values = [101, 103];

        fixed (int* pointer = values)
        {
            WritePointer(pointer, 107);
        }

        Assert.AreEqual(107, values[0]);
    }

    private static int InvokeStaticFunctionPointer()
    {
        IL.Emit.Ldftn(IL.Ref(typeof(FunctionTarget)).Method(nameof(FunctionTarget.GetValue)));
        IL.Emit.Calli(IL.Ref(typeof(FunctionTarget)).Method(nameof(FunctionTarget.GetValue)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int InvokeVirtualFunctionPointer(VirtualTarget target)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(target);
        IL.Emit.Ldvirtftn(IL.Ref(typeof(VirtualTarget)).Method(nameof(VirtualTarget.GetValue)));
        IL.Emit.Calli(IL.Ref(typeof(VirtualTarget)).Method(nameof(VirtualTarget.GetValue)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static unsafe int ReadPointer(int* pointer)
    {
        IL.Emit.Ldarg((nint)pointer);
        IL.Emit.Ldind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static unsafe void WritePointer(int* pointer, int value)
    {
        IL.Emit.Ldarg((nint)pointer);
        IL.Emit.Ldarg(value);
        IL.Emit.Stind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static class FunctionTarget
    {
        public static int GetValue()
        {
            return 79;
        }
    }

    private class VirtualTarget
    {
        public virtual int GetValue()
        {
            return 81;
        }
    }

    private sealed class OverrideTarget : VirtualTarget
    {
        public override int GetValue()
        {
            return 83;
        }
    }
}
