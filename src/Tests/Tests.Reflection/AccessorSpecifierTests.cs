using Nebulae.Reflection;
using Nebulae.Reflection.Extensions;
using Nebulae.Reflection.Specifiers;
using System.Reflection;
using Tests.Reflection.Fixtures;

namespace Tests.Reflection;

[TestClass]
public sealed class AccessorSpecifierTests
{
    [TestMethod]
    public void Property_NonPublicAccessors_AreInvocable()
    {
        var target = new ReflectionTarget(1);
        PropertyInfo property = typeof(ReflectionTarget).Property("HiddenValue")!;
        Reflector<ReflectionTarget>.Close.Get<int> getter = property
            .Specify().Get().Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Get<int>>();
        Reflector<ReflectionTarget>.Close.Set<int> setter = property
            .Specify().Set().Bind(target)
            .Compile<Reflector<ReflectionTarget>.Close.Set<int>>();

        setter(42);

        Assert.AreEqual(42, getter());
    }

    [TestMethod]
    public void Property_IndexerGetter_ForwardsIndexArgument()
    {
        var target = new ReflectionTarget(1);
        PropertyInfo property = typeof(ReflectionTarget).Indexer(typeof(int))!;
        Func<int, string> getter = property
            .Specify().Get().Bind(target)
            .Compile<Func<int, string>>();

        Assert.AreEqual("public:7", getter(7));
    }

    [TestMethod]
    public void Event_NonPublicAddAndRemove_AreInvocable()
    {
        var target = new ReflectionTarget(0);
        EventInfo eventInfo = typeof(ReflectionTarget).Event("HiddenChanged")!;
        Action<EventHandler> add = eventInfo.Specify().Add().Bind(target).Compile<Action<EventHandler>>();
        Action<EventHandler> remove = eventInfo.Specify().Remove().Bind(target).Compile<Action<EventHandler>>();
        int invocationCount = 0;
        EventHandler handler = Handler;

        add(handler);
        target.RaiseHiddenChanged();
        remove(handler);
        target.RaiseHiddenChanged();

        Assert.AreEqual(1, invocationCount);

        void Handler(object? _, EventArgs __) => invocationCount++;
    }

    [TestMethod]
    public void PropertyAndEventSpecifiers_UseMemberIdentityForEquality()
    {
        PropertySpecifier property = typeof(ReflectionTarget).Property(nameof(ReflectionTarget.Value))!.Specify();
        PropertySpecifier sameProperty = typeof(ReflectionTarget).Property(nameof(ReflectionTarget.Value))!.Specify();
        PropertySpecifier otherProperty = typeof(ReflectionTarget).Property(nameof(ReflectionTarget.Label))!.Specify();
        EventSpecifier @event = typeof(ReflectionTarget).Event(nameof(ReflectionTarget.Changed))!.Specify();
        EventSpecifier sameEvent = typeof(ReflectionTarget).Event(nameof(ReflectionTarget.Changed))!.Specify();
        EventSpecifier otherEvent = typeof(ReflectionTarget).Event(nameof(ReflectionTarget.StaticChanged))!.Specify();

        Assert.AreEqual(property, sameProperty);
        Assert.IsTrue(property == sameProperty);
        Assert.AreEqual(property.GetHashCode(), sameProperty.GetHashCode());
        Assert.AreNotEqual(property, otherProperty);
        Assert.IsTrue(property != otherProperty);
        Assert.AreEqual(@event, sameEvent);
        Assert.IsTrue(@event == sameEvent);
        Assert.AreEqual(@event.GetHashCode(), sameEvent.GetHashCode());
        Assert.AreNotEqual(@event, otherEvent);
        Assert.IsTrue(@event != otherEvent);
    }
}
