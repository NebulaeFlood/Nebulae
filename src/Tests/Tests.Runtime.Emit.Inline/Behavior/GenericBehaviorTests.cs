using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline;

namespace Tests.Runtime.Emit.Inline.Behavior;

[TestClass]
public sealed class GenericBehaviorTests
{
    [TestMethod]
    public void GenericMethodsReturnExpectedValues()
    {
        Assert.AreEqual(37, CallGenericIdentity(37));
    }

    [TestMethod]
    public void ConstructedGenericTypeMembersCanBeCalled()
    {
        var target = new GenericBox<int>(0);

        Assert.AreEqual(41, CallConstructedGenericMember(target, 41));
    }

    [TestMethod]
    public void ConstructedGenericTypeStateCanBeAccessed()
    {
        var target = new GenericBox<string>(string.Empty);

        Assert.AreEqual("field value", WriteAndReadGenericField(target, "field value"));
        Assert.AreEqual("property value", WriteAndReadGenericProperty(target, "property value"));
    }

    private static int CallGenericIdentity(int value)
    {
        IL.Emit.Ldarg(value);
        IL.Emit.Call(
            IL.Ref(typeof(GenericMethodTarget))
                .Method(nameof(GenericMethodTarget.Identity), typeof(GenericRef), typeof(GenericRef))
                .MakeGeneric(typeof(int)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static int CallConstructedGenericMember(GenericBox<int> target, int value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Call(
            IL.Ref(typeof(GenericBox<int>))
                .Method(nameof(GenericBox<>.Echo), typeof(GenericRef), typeof(GenericRef)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static string WriteAndReadGenericField(GenericBox<string> target, string value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Stfld(IL.Ref(typeof(GenericBox<string>)).Field(nameof(GenericBox<>.Field)));
        IL.Emit.Ldarg(target);
        IL.Emit.Ldfld(IL.Ref(typeof(GenericBox<string>)).Field(nameof(GenericBox<>.Field)));
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static string WriteAndReadGenericProperty(GenericBox<string> target, string value)
    {
        IL.Emit.Ldarg(target);
        IL.Emit.Ldarg(value);
        IL.Emit.Callvirt(IL.Ref(typeof(GenericBox<string>)).Property(nameof(GenericBox<>.Value)).Set);
        IL.Emit.Ldarg(target);
        IL.Emit.Callvirt(IL.Ref(typeof(GenericBox<string>)).Property(nameof(GenericBox<>.Value)).Get);
        IL.Emit.Ret();
        throw IL.Fail();
    }

    private static class GenericMethodTarget
    {
        public static T Identity<T>(T value)
        {
            return value;
        }
    }

    private sealed class GenericBox<T>(T value)
    {
        public T Field = value;

        public T Value { get; set; } = value;

        public T Echo(T value)
        {
            return value;
        }
    }
}
