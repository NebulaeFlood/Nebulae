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
    public void Subscribe_MulticastDelegate_ForwardsAllHandlersInInvocationOrder()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> handler = target.RecordFirst;
        handler += target.RecordSecond;
        handler += target.RecordThird;

        weakEvent.Subscribe(handler);
        weakEvent.Invoke(sender, new TestEventArgs(7));

        Assert.AreEqual(
            "target-first:7|target-second:7|target-third:7",
            string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Subscribe_ContravariantStaticHandler_ForwardsDerivedArguments()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        TestEventArgs args = new(11);
        EventHandler<object, object> broaderHandler =
            WeakEventTestSupport.RecordContravariantStatic;
        EventHandler<Recorder, TestEventArgs> handler = broaderHandler;

        weakEvent.Subscribe(handler);
        weakEvent.Invoke(sender, args);

        Assert.AreEqual(
            "contravariant-static:11",
            string.Join('|', sender.Entries));
    }

    [TestMethod]
    public void Subscribe_ContravariantInstanceHandler_ForwardsDerivedArguments()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        TestEventArgs args = new(13);
        RecordingTarget target = new("contravariant-instance");
        EventHandler<object, object> broaderHandler = target.RecordContravariant;
        EventHandler<Recorder, TestEventArgs> handler = broaderHandler;

        weakEvent.Subscribe(handler);
        weakEvent.Invoke(sender, args);

        Assert.AreEqual(
            "contravariant-instance:13",
            string.Join('|', sender.Entries));
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
    public void Unsubscribe_MulticastDelegate_RemovesMostRecentMatchingSequence()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> sequence = target.RecordFirst;
        sequence += target.RecordSecond;

        weakEvent.Subscribe(sequence);
        weakEvent.Subscribe(target.RecordThird);
        weakEvent.Subscribe(sequence);

        weakEvent.Unsubscribe(sequence);
        weakEvent.Invoke(sender, new TestEventArgs(5));

        Assert.AreEqual(
            "target-first:5|target-second:5|target-third:5",
            string.Join('|', sender.Entries));
        GC.KeepAlive(target);
    }

    [TestMethod]
    public void Unsubscribe_MulticastDelegateWithoutContiguousMatch_PreservesSubscriptions()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> sequence = target.RecordFirst;
        sequence += target.RecordSecond;

        weakEvent.Subscribe(target.RecordFirst);
        weakEvent.Subscribe(target.RecordThird);
        weakEvent.Subscribe(target.RecordSecond);

        weakEvent.Unsubscribe(sequence);
        weakEvent.Invoke(sender, new TestEventArgs(6));

        Assert.AreEqual(
            "target-first:6|target-third:6|target-second:6",
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
    public void PublicEntryPoints_NullOperands_ThrowArgumentNullException()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        WeakEvent<Recorder, TestEventArgs> nullEvent = null!;
        EventHandler<Recorder, TestEventArgs> handler = WeakEventTestSupport.RecordStatic;

        AssertNullHandler(
            () => weakEvent.Subscribe((EventHandler<Recorder, TestEventArgs>)null!));
        AssertNullHandler(
            () => weakEvent.Unsubscribe((EventHandler<Recorder, TestEventArgs>)null!));
        AssertNullHandler(() => _ = weakEvent + (EventHandler<Recorder, TestEventArgs>)null!);
        AssertNullHandler(() => _ = weakEvent - (EventHandler<Recorder, TestEventArgs>)null!);

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullEvent + handler);
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = nullEvent - handler);
    }

    [TestMethod]
    public void Operators_ValidHandlers_ReturnSameInstanceAndMirrorMethods()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget target = new("target");
        EventHandler<Recorder, TestEventArgs> first = target.RecordFirst;
        EventHandler<Recorder, TestEventArgs> second = target.RecordSecond;

        WeakEvent<Recorder, TestEventArgs> result = weakEvent + first;
        Assert.AreSame(weakEvent, result);

        result = weakEvent + second;
        Assert.AreSame(weakEvent, result);

        result = weakEvent - first;
        Assert.AreSame(weakEvent, result);

        weakEvent.Invoke(sender, new TestEventArgs(17));
        Assert.AreEqual("target-second:17", string.Join('|', sender.Entries));
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

}
