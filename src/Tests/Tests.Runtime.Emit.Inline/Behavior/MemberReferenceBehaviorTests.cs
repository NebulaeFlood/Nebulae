using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class MemberReferenceBehaviorTests
{
    [TestMethod]
    public void StaticAndInstanceMethodsCanBeCalled()
    {
        var target = new MemberTarget(10);

        Assert.AreEqual(6, CallStaticMethod(5));
        Assert.AreEqual(15, CallInstanceMethod(target, 5));
    }

    [TestMethod]
    public void InterfaceDispatchReturnsExpectedValue()
    {
        IValueTransformer target = new MemberTarget(7);

        Assert.AreEqual(21, CallInterfaceMethod(target, 3));
    }

    [TestMethod]
    public void FieldReadAndWriteWork()
    {
        var target = new MemberTarget(0);

        Assert.AreEqual(31, WriteAndReadInstanceField(target, 31));
        Assert.AreEqual(47, WriteAndReadStaticField(47));
    }

    [TestMethod]
    public void PropertyAccessorsWork()
    {
        var target = new MemberTarget(0);

        Assert.AreEqual(59, WriteAndReadProperty(target, 59));
    }

    [TestMethod]
    public void ConstructorsCreateInitializedInstances()
    {
        MemberTarget target = CreateTarget(71);

        Assert.AreEqual(71, target.Value);
        Assert.IsNotNull(CreateObject());
    }

    private static int CallStaticMethod(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Call(IL.Ref(typeof(MemberTarget)).Method(nameof(MemberTarget.Increment), typeof(int), typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int CallInstanceMethod(MemberTarget target, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Call(IL.Ref(typeof(MemberTarget)).Method(nameof(MemberTarget.AddToValue), typeof(int), typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int CallInterfaceMethod(IValueTransformer target, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Callvirt(IL.Ref(typeof(IValueTransformer)).Method(nameof(IValueTransformer.Transform), typeof(int), typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int WriteAndReadInstanceField(MemberTarget target, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Stfld(IL.Ref(typeof(MemberTarget)).Field(nameof(MemberTarget.InstanceField)));
        IL.Emit.Ldarg(target);
        IL.Emit.Ldfld(IL.Ref(typeof(MemberTarget)).Field(nameof(MemberTarget.InstanceField)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int WriteAndReadStaticField(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Stsfld(IL.Ref(typeof(MemberTarget)).Field(nameof(MemberTarget.StaticField)));
        IL.Emit.Ldsfld(IL.Ref(typeof(MemberTarget)).Field(nameof(MemberTarget.StaticField)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int WriteAndReadProperty(MemberTarget target, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Property(nameof(MemberTarget.Value)).Set);
        IL.Emit.Ldarg(target);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Property(nameof(MemberTarget.Value)).Get);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static MemberTarget CreateTarget(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Newobj(IL.Ref(typeof(MemberTarget)).Constructor(typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static object CreateObject()
    {
        IL.Emit.Newobj(IL.Ref(typeof(object)).Constructor(Type.EmptyTypes));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private interface IValueTransformer
    {
        int Transform(int value);
    }

    private sealed class MemberTarget(int value) : IValueTransformer
    {
        public static int StaticField = 0;

        public int InstanceField = value;

        public int Value { get; set; } = value;

        public static int Increment(int value)
        {
            return value + 1;
        }

        public int AddToValue(int value)
        {
            return Value + value;
        }

        public int Transform(int value)
        {
            return Value * value;
        }
    }
}
