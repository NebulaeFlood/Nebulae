using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class ArrayAndManagedMemoryBehaviorTests
{
    [TestMethod]
    public void IntegerArrayElementsCanBeReadAndWritten()
    {
        int[] values = [10, 20, 30];

        Assert.AreEqual(20, ReadIntegerElement(values, 1));
        WriteIntegerElement(values, 1, 25);
        Assert.AreEqual(25, values[1]);
    }

    [TestMethod]
    public void ReferenceArrayElementsCanBeReadAndWritten()
    {
        string[] values = ["first", "second"];

        Assert.AreEqual("second", ReadReferenceElement(values, 1));
        WriteReferenceElement(values, 0, "updated");
        Assert.AreEqual("updated", values[0]);
    }

    [TestMethod]
    public void RefParametersCanBeReadAndWritten()
    {
        int value = 43;

        Assert.AreEqual(43, ReadRefValue(ref value));
        WriteRefValue(ref value, 47);
        Assert.AreEqual(47, value);
    }

    [TestMethod]
    public void OutParametersReceiveValues()
    {
        WriteOutValue(out int value);

        Assert.AreEqual(53, value);
    }

    [TestMethod]
    public void ArrayElementAddressesCanBeReadAndWritten()
    {
        int[] values = [59];

        Assert.AreEqual(61, WriteAndReadElementAddress(values, 0, 61));
        Assert.AreEqual(61, values[0]);
    }

    private static int ReadIntegerElement(int[] values, int index)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldelem_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void WriteIntegerElement(int[] values, int index, int value)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldarg(value);
        IL.Emit.Stelem_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static string ReadReferenceElement(string[] values, int index)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldelem_Ref();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void WriteReferenceElement(string[] values, int index, string value)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldarg(value);
        IL.Emit.Stelem_Ref();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int ReadRefValue(ref int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Ldind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void WriteRefValue(ref int value, int newValue)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Ldarg(newValue);
        IL.Emit.Stind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void WriteOutValue(out int value)
    {
        IL.Emit.Ldarg(out value);
        IL.Emit.Ldc_I4(53);
        IL.Emit.Stind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int WriteAndReadElementAddress(int[] values, int index, int value)
    {
        IL.Emit.Ldarg(values);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldelema(typeof(int));
        IL.Emit.Dup();
        IL.Emit.Ldarg(value);
        IL.Emit.Stind_I4();
        IL.Emit.Ldind_I4();
        IL.Emit.Ret();
        throw IL.Fail();
    }
}
