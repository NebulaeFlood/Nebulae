using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Lifetime.WeakEvents;

namespace Tests.Lifetime.WeakEvents;

[TestClass]
public sealed class WeakEventTests
{
    [TestMethod]
    public void Invoke_NoSubscribers_CompletesWithoutObservableEffect()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();

        weakEvent.Invoke(new Recorder(), new TestEventArgs(1));
    }

    [TestMethod]
    public void Subscribe_MixedHandlers_ForwardsArgumentsInSubscriptionOrder()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        TestEventArgs args = new(42);
        RecordingTarget target = new("instance");

        weakEvent.Subscribe(WeakEventTestSupport.RecordStatic);
        weakEvent.Subscribe(target.Record);

        weakEvent.Invoke(sender, args);

        Assert.AreEqual("static:42|instance:42", string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Subscribe_MulticastDelegate_SubscribesOnlyLastHandler()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> handler = target.RecordFirst;
        handler += target.RecordSecond;
        handler += target.RecordThird;

        weakEvent.Subscribe(handler);
        weakEvent.Invoke(sender, new TestEventArgs(7));

        Assert.AreEqual("target-third:7", string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Unsubscribe_InterleavedDuplicates_RemovesMostRecentMatch()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> handlerA = target.RecordFirst;
        EventHandler<Recorder, TestEventArgs> handlerB = target.RecordSecond;

        weakEvent.Subscribe(handlerA);
        weakEvent.Subscribe(handlerB);
        weakEvent.Subscribe(handlerA);

        weakEvent.Unsubscribe(handlerA);
        weakEvent.Invoke(sender, new TestEventArgs(3));

        Assert.AreEqual(
            "target-first:3|target-second:3",
            string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Unsubscribe_UnknownHandler_PreservesExistingSubscriptions()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget subscribed = new("subscribed");
        RecordingTarget unknown = new("unknown");

        weakEvent.Subscribe(subscribed.Record);
        weakEvent.Unsubscribe((EventHandler<Recorder, TestEventArgs>)unknown.Record);
        weakEvent.Invoke(sender, new TestEventArgs(5));

        Assert.AreEqual("subscribed:5", string.Join('|', sender.Entries));
        GC.KeepAlive(subscribed);
        GC.KeepAlive(unknown);
    }

    [TestMethod]
    public void Subscribe_CompatibleCustomDelegate_ForwardsArguments()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("custom");
        CompatibleHandler handler = target.Record;

        weakEvent.Subscribe((Delegate)handler);
        weakEvent.Invoke(sender, new TestEventArgs(11));

        Assert.AreEqual("custom:11", string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Subscribe_IncompatibleDelegate_ThrowsWithoutChangingState()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        Action incompatible = IncompatibleHandler;
        weakEvent.Subscribe(WeakEventTestSupport.RecordStatic);

        Assert.ThrowsExactly<ArgumentException>(
            () => weakEvent.Subscribe((Delegate)incompatible));

        weakEvent.Invoke(sender, new TestEventArgs(13));
        Assert.AreEqual("static:13", string.Join('|', sender.Entries));
    }

    [TestMethod]
    public void PublicEntryPoints_NullOperands_ThrowArgumentNullException()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        WeakEvent<Recorder, TestEventArgs> nullEvent = null!;
        EventHandler<Recorder, TestEventArgs> handler = WeakEventTestSupport.RecordStatic;
        Delegate delegateHandler = handler;

        AssertNullHandler(
            () => weakEvent.Subscribe((EventHandler<Recorder, TestEventArgs>)null!));
        AssertNullHandler(() => weakEvent.Subscribe((Delegate)null!));
        AssertNullHandler(() => weakEvent.Unsubscribe(null!));
        AssertNullHandler(() => _ = weakEvent + (EventHandler<Recorder, TestEventArgs>)null!);
        AssertNullHandler(() => _ = weakEvent + (Delegate)null!);
        AssertNullHandler(() => _ = weakEvent - (Delegate)null!);

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullEvent + handler);
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullEvent + delegateHandler);
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullEvent - delegateHandler);
    }

    [TestMethod]
    public void Operators_ValidHandlers_ReturnSameInstanceAndMirrorMethods()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> typedHandler = target.RecordFirst;
        CompatibleHandler customHandler = target.Record;

        WeakEvent<Recorder, TestEventArgs> result = weakEvent + typedHandler;
        Assert.AreSame(weakEvent, result);

        result = weakEvent + (Delegate)customHandler;
        Assert.AreSame(weakEvent, result);

        result = weakEvent - typedHandler;
        Assert.AreSame(weakEvent, result);

        weakEvent.Invoke(sender, new TestEventArgs(17));
        Assert.AreEqual("target:17", string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

#if NET10_0_OR_GREATER
    [TestMethod]
    public void Invoke_RefStructTypeArguments_ForwardsSpansToStaticHandler()
    {
        WeakEvent<Span<int>, ReadOnlySpan<int>> weakEvent = new();
        Span<int> sender = stackalloc int[1];
        ReadOnlySpan<int> args = [19, 23];

        weakEvent.Subscribe(RecordSpans);
        weakEvent.Invoke(sender, args);

        Assert.AreEqual(42, sender[0]);
    }

    private static void RecordSpans(Span<int> sender, ReadOnlySpan<int> args)
    {
        sender[0] = args[0] + args[1];
    }
#endif

    private static void AssertNullHandler(Action action)
    {
        ArgumentNullException exception =
            Assert.ThrowsExactly<ArgumentNullException>(action);
        Assert.AreEqual("handler", exception.ParamName);
    }

    private static void IncompatibleHandler()
    {
    }

    private delegate void CompatibleHandler(Recorder sender, TestEventArgs args);
}
