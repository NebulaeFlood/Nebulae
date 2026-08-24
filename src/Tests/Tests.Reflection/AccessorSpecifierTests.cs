using Nebulae.Reflection;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class AccessorSpecifierTests
{
    [TestMethod]
    public void Property_GetAndSet_ReadAndModifyValue()
    {
        var target = new ReflectionTarget(1);
        PropertyInfo property = typeof(ReflectionTarget)
            .GetProperty(nameof(ReflectionTarget.Value), Reflector.DefaultLookup)!;
        Reflector<ReflectionTarget>.Close.Get<int> getter = property
            .Specify()
            .Get()
            .Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = property
            .Specify()
            .Set()
            .Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();

        setter(42);

        Assert.AreEqual(42, getter());
        Assert.AreEqual(42, target.Value);
    }

    [TestMethod]
    public void Property_NonPublicAccessors_ReadAndModifyValue()
    {
        var target = new ReflectionTarget(1);
        PropertyInfo property = typeof(ReflectionTarget)
            .GetProperty("HiddenValue", Reflector.DefaultLookup)!;
        Reflector<ReflectionTarget>.Close.Get<int> getter = property
            .Specify()
            .Get()
            .Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = property
            .Specify()
            .Set()
            .Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();

        setter(42);

        Assert.AreEqual(42, getter());
    }

    [TestMethod]
    public void Event_AddAndRemove_ControlObservableSubscription()
    {
        var target = new ReflectionTarget(0);
        EventInfo eventInfo = typeof(ReflectionTarget)
            .GetEvent(nameof(ReflectionTarget.Changed), Reflector.DefaultLookup)!;
        Action<EventHandler> add = eventInfo.Specify().Add().Bind(target).Compile<Action<EventHandler>>();
        Action<EventHandler> remove = eventInfo.Specify().Remove().Bind(target).Compile<Action<EventHandler>>();
        int invocationCount = 0;
        EventHandler handler = Handler;

        add(handler);
        target.RaiseChanged();
        remove(handler);
        target.RaiseChanged();

        Assert.AreEqual(1, invocationCount);

        void Handler(object? _, EventArgs __) => invocationCount++;
    }
}
