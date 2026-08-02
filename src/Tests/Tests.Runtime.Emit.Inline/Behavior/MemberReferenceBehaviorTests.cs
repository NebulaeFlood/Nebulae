using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class MemberReferenceBehaviorTests
{
    [TestMethod]
    public void StaticAndInstanceMethods_CanBeCalled()
    {
        var target = new MemberTarget(10);

        Assert.AreEqual(6, CallStaticMethod(5));
        Assert.AreEqual(15, CallInstanceMethod(target, 5));
    }

    [TestMethod]
    public void InterfaceDispatch_ReturnsExpectedValue()
    {
        IValueTransformer target = new MemberTarget(7);

        Assert.AreEqual(21, CallInterfaceMethod(target, 3));
    }

    [TestMethod]
    public void FieldReference_CanReadAndWrite()
    {
        var target = new MemberTarget(0);

        Assert.AreEqual(31, WriteAndReadInstanceField(target, 31));
        Assert.AreEqual(47, WriteAndReadStaticField(47));
    }

    [TestMethod]
    public void PropertyAccessors_CanReadAndWrite()
    {
        var target = new MemberTarget(0);

        Assert.AreEqual(59, WriteAndReadProperty(target, 59));
    }

    [TestMethod]
    public void ConstructorReference_CreatesInitializedInstance()
    {
        MemberTarget target = CreateTarget(71);

        Assert.AreEqual(71, target.Value);
        Assert.IsNotNull(CreateObject());
    }

    [TestMethod]
    public void EventAccessors_AddAndRemoveHandlers()
    {
        var target = new MemberTarget(0);
        int received = 0;

        void Handler(int value) => received += value;

        AddHandler(target, Handler);
        target.RaiseChanged(43);
        Assert.AreEqual(43, received);

        RemoveHandler(target, Handler);
        target.RaiseChanged(47);
        Assert.AreEqual(43, received);
    }

    [TestMethod]
    public void IndexerAccessors_ReadAndWriteValues()
    {
        var target = new MemberTarget(0);

        Assert.AreEqual(53, WriteAndReadIndexer(target, 2, 53));
    }

    [TestMethod]
    public void MethodSignature_SelectsExpectedOverload()
    {
        Assert.AreEqual(61, CallIntegerOverload(60));
        Assert.AreEqual("selected", CallStringOverload("selected"));
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

    private static void AddHandler(MemberTarget target, Action<int> handler)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(handler);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Event(nameof(MemberTarget.Changed)).Add);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static void RemoveHandler(MemberTarget target, Action<int> handler)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(handler);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Event(nameof(MemberTarget.Changed)).Remove);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int WriteAndReadIndexer(MemberTarget target, int index, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(index);
        IL.Emit.Ldarg(value);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Indexer(typeof(int)).Set);
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(index);
        IL.Emit.Callvirt(IL.Ref(typeof(MemberTarget)).Indexer(typeof(int)).Get);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int CallIntegerOverload(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Call(IL.Ref(typeof(MemberTarget)).Method(nameof(MemberTarget.Select), typeof(int), typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static string CallStringOverload(string value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Call(IL.Ref(typeof(MemberTarget)).Method(nameof(MemberTarget.Select), typeof(string), typeof(string)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private interface IValueTransformer
    {
        int Transform(int value);
    }

    private sealed class MemberTarget(int value) : IValueTransformer
    {
        private readonly Dictionary<int, int> _values = [];

        public static int StaticField = 0;

        public int InstanceField = value;

        public int Value { get; set; } = value;

        public event Action<int>? Changed;

        public int this[int index]
        {
            get => _values[index];
            set => _values[index] = value;
        }

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

        public void RaiseChanged(int value) => Changed?.Invoke(value);

        public static int Select(int value) => value + 1;

        public static string Select(string value) => value;
    }
}
