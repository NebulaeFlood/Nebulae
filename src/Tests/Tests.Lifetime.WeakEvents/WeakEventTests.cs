using System.Reflection;
using System.Runtime.CompilerServices;
using Nebulae.Lifetime.WeakEvents;

#if NET9_0_OR_GREATER
using WeakEventCallback = System.EventHandler<object, System.EventArgs>;
#else
using WeakEventCallback = Nebulae.Lifetime.WeakEvents.EventHandler<object, System.EventArgs>;
#endif

namespace Tests.Lifetime.WeakEvents;

[TestClass]
public sealed class WeakEventTests
{
    private static readonly int[] s_expectedSubscriptionOrder = [1, 2, 3];
    private static readonly int[] s_expectedMulticastCalls = [2];

    [TestMethod]
    public void InvokeWithoutSubscriptions_CompletesWithoutSideEffects()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();

        weakEvent.Invoke(new object(), EventArgs.Empty);
    }

    [TestMethod]
    public void Subscribe_StaticAndInstanceHandlersReceiveOriginalArguments()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var sender = new object();
        var args = new EventArgs();
        var receiver = new ArgumentReceiver();
        WeakEventCallback instanceHandler = receiver.Handle;

        StaticArgumentReceiver.Reset();
        weakEvent.Subscribe(StaticArgumentReceiver.Handle);
        weakEvent.Subscribe(instanceHandler);

        weakEvent.Invoke(sender, args);

        Assert.AreSame(sender, StaticArgumentReceiver.Sender);
        Assert.AreSame(args, StaticArgumentReceiver.Args);
        Assert.AreSame(sender, receiver.Sender);
        Assert.AreSame(args, receiver.Args);
        GC.KeepAlive(instanceHandler);
    }

    [TestMethod]
    public void Invoke_MultipleHandlersRunInSubscriptionOrder()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var calls = new List<int>();
        WeakEventCallback first = (_, _) => calls.Add(1);
        WeakEventCallback second = (_, _) => calls.Add(2);
        WeakEventCallback third = (_, _) => calls.Add(3);

        weakEvent.Subscribe(first);
        weakEvent.Subscribe(second);
        weakEvent.Subscribe(third);

        weakEvent.Invoke(new object(), EventArgs.Empty);

        CollectionAssert.AreEqual(s_expectedSubscriptionOrder, calls);
        GC.KeepAlive(first);
        GC.KeepAlive(second);
        GC.KeepAlive(third);
    }

    [TestMethod]
    public void Unsubscribe_DuplicateRegistrationRemovesOnlyLastMatch()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var receiver = new CountingReceiver();
        WeakEventCallback handler = receiver.Handle;

        weakEvent.Subscribe(handler);
        weakEvent.Subscribe(handler);
        weakEvent.Unsubscribe(handler);

        weakEvent.Invoke(new object(), EventArgs.Empty);
        Assert.AreEqual(1, receiver.Calls);

        weakEvent.Unsubscribe(handler);
        weakEvent.Invoke(new object(), EventArgs.Empty);
        Assert.AreEqual(1, receiver.Calls);
        GC.KeepAlive(handler);
    }

    [TestMethod]
    public void Subscribe_CompatibleDelegateIsInvoked()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var sender = new object();
        var args = new EventArgs();
        object? receivedSender = null;
        EventArgs? receivedArgs = null;
        CompatibleHandler handler = (actualSender, actualArgs) =>
        {
            receivedSender = actualSender;
            receivedArgs = actualArgs;
        };

        weakEvent.Subscribe((Delegate)handler);
        weakEvent.Invoke(sender, args);

        Assert.AreSame(sender, receivedSender);
        Assert.AreSame(args, receivedArgs);
        GC.KeepAlive(handler);
    }

    [TestMethod]
    public void Subscribe_MulticastDelegateUsesOnlyLastHandler()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var calls = new List<int>();
        CompatibleHandler first = (_, _) => calls.Add(1);
        CompatibleHandler second = (_, _) => calls.Add(2);
        Delegate multicast = Delegate.Combine(first, second);

        weakEvent.Subscribe(multicast);
        weakEvent.Invoke(new object(), EventArgs.Empty);

        CollectionAssert.AreEqual(s_expectedMulticastCalls, calls);
        GC.KeepAlive(multicast);
    }

    [TestMethod]
    public void CollectedInstanceSubscriber_IsNotKeptAliveOrInvoked()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var counter = new CallCounter();
        WeakReference<CountingReceiver> subscriber = SubscribeTemporaryReceiver(weakEvent, counter);

        CollectUntilDead(subscriber);
        weakEvent.Invoke(new object(), EventArgs.Empty);

        Assert.IsFalse(subscriber.TryGetTarget(out _));
        Assert.AreEqual(0, counter.Value);
    }

    [TestMethod]
    public void Purge_RemovesCollectedSubscribersAndPreservesLiveSubscribers()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var deadCounter = new CallCounter();
        var liveReceiver = new CountingReceiver();
        WeakEventCallback liveHandler = liveReceiver.Handle;
        WeakReference<CountingReceiver> deadSubscriber = SubscribeTemporaryReceiver(weakEvent, deadCounter);
        weakEvent.Subscribe(liveHandler);

        Assert.AreEqual(2, GetSubscriptionCount(weakEvent));
        CollectUntilDead(deadSubscriber);

        weakEvent.Purge();
        weakEvent.Invoke(new object(), EventArgs.Empty);

        Assert.AreEqual(1, GetSubscriptionCount(weakEvent));
        Assert.AreEqual(0, deadCounter.Value);
        Assert.AreEqual(1, liveReceiver.Calls);
        GC.KeepAlive(liveHandler);
    }

    [TestMethod]
    public void Operators_AddAndRemoveTypedAndCompatibleHandlersOnSameEventInstance()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var receiver = new CountingReceiver();
        WeakEventCallback handler = receiver.Handle;
        CompatibleHandler compatibleHandler = receiver.Handle;

        WeakEvent<object, EventArgs> afterAddition = weakEvent + handler;
        afterAddition.Invoke(new object(), EventArgs.Empty);

        Assert.AreSame(weakEvent, afterAddition);
        Assert.AreEqual(1, receiver.Calls);

        WeakEvent<object, EventArgs> afterRemoval = afterAddition - handler;
        afterRemoval.Invoke(new object(), EventArgs.Empty);

        Assert.AreSame(weakEvent, afterRemoval);
        Assert.AreEqual(1, receiver.Calls);

        WeakEvent<object, EventArgs> afterDelegateAddition = afterRemoval + (Delegate)compatibleHandler;
        afterDelegateAddition.Invoke(new object(), EventArgs.Empty);

        Assert.AreSame(weakEvent, afterDelegateAddition);
        Assert.AreEqual(2, receiver.Calls);

        WeakEvent<object, EventArgs> afterDelegateRemoval = afterDelegateAddition - compatibleHandler;
        afterDelegateRemoval.Invoke(new object(), EventArgs.Empty);

        Assert.AreSame(weakEvent, afterDelegateRemoval);
        Assert.AreEqual(2, receiver.Calls);
        GC.KeepAlive(handler);
        GC.KeepAlive(compatibleHandler);
    }

    [TestMethod]
    public void Methods_NullHandlersThrowArgumentNullException()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();

        Assert.ThrowsExactly<ArgumentNullException>(
            () => weakEvent.Subscribe((WeakEventCallback)null!));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => weakEvent.Subscribe((Delegate)null!));
        Assert.ThrowsExactly<ArgumentNullException>(
            () => weakEvent.Unsubscribe(null!));
    }

    [TestMethod]
    public void Operators_NullOperandsThrowArgumentNullException()
    {
        var weakEvent = new WeakEvent<object, EventArgs>();
        var receiver = new CountingReceiver();
        WeakEventCallback handler = receiver.Handle;
        WeakEvent<object, EventArgs> missingEvent = null!;

        Assert.ThrowsExactly<ArgumentNullException>(
            () => _ = missingEvent + handler);
        Assert.ThrowsExactly<ArgumentNullException>(
            () => _ = weakEvent + (WeakEventCallback)null!);
        Assert.ThrowsExactly<ArgumentNullException>(
            () => _ = weakEvent + (Delegate)null!);
        Assert.ThrowsExactly<ArgumentNullException>(
            () => _ = weakEvent - (Delegate)null!);
        GC.KeepAlive(handler);
    }

    [TestMethod]
    public async Task ConcurrentSubscriptionsAndUnsubscriptions_PreserveAllStateChanges()
    {
        const int operationCount = 64;
        var weakEvent = new WeakEvent<object, EventArgs>();
        var receiver = new CountingReceiver();
        WeakEventCallback handler = receiver.Handle;

        await RunConcurrently(operationCount, () => weakEvent.Subscribe(handler));

        weakEvent.Invoke(new object(), EventArgs.Empty);
        Assert.AreEqual(operationCount, receiver.Calls);

        await RunConcurrently(operationCount, () => weakEvent.Unsubscribe(handler));

        receiver.Reset();
        weakEvent.Invoke(new object(), EventArgs.Empty);
        Assert.AreEqual(0, receiver.Calls);
        GC.KeepAlive(handler);
    }

    private static async Task RunConcurrently(int operationCount, Action operation)
    {
        using var startGate = new ManualResetEventSlim();
        Task[] operations =
        [
            .. Enumerable.Range(0, operationCount)
                .Select(_ => Task.Run(() =>
                {
                    startGate.Wait();
                    operation();
                }))
        ];

        startGate.Set();
        await Task.WhenAll(operations);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<CountingReceiver> SubscribeTemporaryReceiver(
        WeakEvent<object, EventArgs> weakEvent,
        CallCounter counter)
    {
        var receiver = new CountingReceiver(counter);
        weakEvent.Subscribe(receiver.Handle);
        return new WeakReference<CountingReceiver>(receiver);
    }

    private static void CollectUntilDead(WeakReference<CountingReceiver> reference)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        GC.KeepAlive(reference);
    }

    private static int GetSubscriptionCount(WeakEvent<object, EventArgs> weakEvent)
    {
        FieldInfo stateField = typeof(WeakEvent<object, EventArgs>).GetField(
            "_state",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        object state = stateField.GetValue(weakEvent)!;
        FieldInfo countField = state.GetType().GetField(
            "Count",
            BindingFlags.Instance | BindingFlags.Public)!;

        return (int)countField.GetValue(state)!;
    }

    private delegate void CompatibleHandler(object sender, EventArgs args);

    private sealed class ArgumentReceiver
    {
        public object? Sender { get; private set; }

        public EventArgs? Args { get; private set; }

        public void Handle(object sender, EventArgs args)
        {
            Sender = sender;
            Args = args;
        }
    }

    private static class StaticArgumentReceiver
    {
        public static object? Sender { get; private set; }

        public static EventArgs? Args { get; private set; }

        public static void Handle(object sender, EventArgs args)
        {
            Sender = sender;
            Args = args;
        }

        public static void Reset()
        {
            Sender = null;
            Args = null;
        }
    }

    private sealed class CountingReceiver
    {
        private readonly CallCounter? _counter;
        private int _calls;

        public CountingReceiver() { }

        public CountingReceiver(CallCounter counter)
        {
            _counter = counter;
        }

        public int Calls => Volatile.Read(ref _calls);

        public void Handle(object sender, EventArgs args)
        {
            Interlocked.Increment(ref _calls);
            _counter?.Increment();
        }

        public void Reset()
        {
            Volatile.Write(ref _calls, 0);
        }
    }

    private sealed class CallCounter
    {
        private int _value;

        public int Value => Volatile.Read(ref _value);

        public void Increment()
        {
            Interlocked.Increment(ref _value);
        }
    }
}
