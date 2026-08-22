using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Lifetime.WeakEvents;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Tests.Lifetime.WeakEvents;

[TestClass]
public sealed class WeakEventConcurrencyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public TestContext TestContext { get; set; } = null!;

    [TestMethod]
    public async Task ConcurrentSubscribeAndUnsubscribe_PreservesAllSuccessfulUpdates()
    {
        const int HandlerCount = 128;
        WeakEvent<object, TestEventArgs> weakEvent = new();
        CountingTarget[] removedTargets = CreateTargets(HandlerCount);
        CountingTarget[] addedTargets = CreateTargets(HandlerCount);
        EventHandler<object, TestEventArgs>[] removedHandlers = CreateHandlers(removedTargets);
        EventHandler<object, TestEventArgs>[] addedHandlers = CreateHandlers(addedTargets);
        CancellationToken cancellationToken = TestContext.CancellationToken;

        foreach (EventHandler<object, TestEventArgs> handler in removedHandlers)
        {
            weakEvent.Subscribe(handler);
        }

        using ManualResetEventSlim start = new(false);
        using CountdownEvent ready = new(2);
        Task subscribe = Task.Run(() =>
        {
            ready.Signal();
            start.Wait(cancellationToken);
            foreach (EventHandler<object, TestEventArgs> handler in addedHandlers)
            {
                weakEvent.Subscribe(handler);
            }
        }, cancellationToken);
        Task unsubscribe = Task.Run(() =>
        {
            ready.Signal();
            start.Wait(cancellationToken);
            foreach (EventHandler<object, TestEventArgs> handler in removedHandlers)
            {
                weakEvent.Unsubscribe(handler);
            }
        }, cancellationToken);

        bool workersReady = ready.Wait(Timeout, cancellationToken);
        start.Set();
        Assert.IsTrue(workersReady);
        await Task.WhenAll(subscribe, unsubscribe).WaitAsync(Timeout, cancellationToken);

        weakEvent.Invoke(new object(), new TestEventArgs(0));

        AssertAllCounts(removedTargets, 0);
        AssertAllCounts(addedTargets, 1);
        GC.KeepAlive(removedTargets);
        GC.KeepAlive(addedTargets);
    }

    [TestMethod]
    public async Task ConcurrentInvoke_StableSubscriptions_DeliversExactlyOncePerInvocation()
    {
        const int HandlerCount = 32;
        const int WorkerCount = 8;
        const int InvocationsPerWorker = 100;
        WeakEvent<object, TestEventArgs> weakEvent = new();
        CountingTarget[] targets = CreateTargets(HandlerCount);
        CancellationToken cancellationToken = TestContext.CancellationToken;

        foreach (CountingTarget target in targets)
        {
            weakEvent.Subscribe(target.Handle);
        }

        using ManualResetEventSlim start = new(false);
        using CountdownEvent ready = new(WorkerCount);
        Task[] workers = new Task[WorkerCount];
        for (int i = 0; i < workers.Length; i++)
        {
            workers[i] = Task.Run(() =>
            {
                ready.Signal();
                start.Wait(cancellationToken);
                for (int invocation = 0; invocation < InvocationsPerWorker; invocation++)
                {
                    weakEvent.Invoke(new object(), new TestEventArgs(invocation));
                }
            }, cancellationToken);
        }

        bool workersReady = ready.Wait(Timeout, cancellationToken);
        start.Set();
        Assert.IsTrue(workersReady);
        await Task.WhenAll(workers).WaitAsync(Timeout, cancellationToken);

        AssertAllCounts(targets, WorkerCount * InvocationsPerWorker);
        GC.KeepAlive(targets);
    }

    [TestMethod]
    public async Task ConcurrentPurgeAndSubscribe_DoesNotLoseLiveSubscriptions()
    {
        const int IterationCount = 8;
        const int HandlerCount = 64;

        for (int iteration = 0; iteration < IterationCount; iteration++)
        {
            WeakEvent<object, TestEventArgs> weakEvent = new();
            CancellationToken cancellationToken = TestContext.CancellationToken;
            WeakReference[] collected = new WeakReference[HandlerCount];
            for (int i = 0; i < collected.Length; i++)
            {
                collected[i] = SubscribeTemporaryCountingTarget(weakEvent);
            }

            WeakEventLifetimeTests.CollectUntilDead(collected);
            Assert.IsFalse(Array.Exists(collected, static reference => reference.IsAlive));

            CountingTarget[] addedTargets = CreateTargets(HandlerCount);
            EventHandler<object, TestEventArgs>[] addedHandlers = CreateHandlers(addedTargets);
            using ManualResetEventSlim start = new(false);
            using CountdownEvent ready = new(2);
            Task purge = Task.Run(() =>
            {
                ready.Signal();
                start.Wait(cancellationToken);
                weakEvent.Purge();
            }, cancellationToken);
            Task subscribe = Task.Run(() =>
            {
                ready.Signal();
                start.Wait(cancellationToken);
                foreach (EventHandler<object, TestEventArgs> handler in addedHandlers)
                {
                    weakEvent.Subscribe(handler);
                }
            }, cancellationToken);

            bool workersReady = ready.Wait(Timeout, cancellationToken);
            start.Set();
            Assert.IsTrue(workersReady);
            await Task.WhenAll(purge, subscribe).WaitAsync(Timeout, cancellationToken);

            weakEvent.Invoke(new object(), new TestEventArgs(iteration));
            AssertAllCounts(addedTargets, 1);
            GC.KeepAlive(addedTargets);
        }
    }

    [TestMethod]
    public async Task CompletedHandlerChanges_AreVisibleToSubsequentInvokeAcrossThreads()
    {
        const int RoundCount = 64;
        WeakEvent<object, TestEventArgs> weakEvent = new();
        CountingTarget stableTarget = new();
        InvocationRecorder changingTarget = new();
        EventHandler<object, TestEventArgs> changingHandler = changingTarget.Handle;
        CancellationToken cancellationToken = TestContext.CancellationToken;
        weakEvent.Subscribe(stableTarget.Handle);

        using SemaphoreSlim subscribed = new(0);
        using SemaphoreSlim invokedWhileSubscribed = new(0);
        using SemaphoreSlim unsubscribed = new(0);
        using SemaphoreSlim invokedWhileUnsubscribed = new(0);

        Task modifier = Task.Run(async () =>
        {
            for (int round = 0; round < RoundCount; round++)
            {
                weakEvent.Subscribe(changingHandler);
                subscribed.Release();
                await invokedWhileSubscribed.WaitAsync(Timeout, cancellationToken);

                weakEvent.Unsubscribe(changingHandler);
                unsubscribed.Release();
                await invokedWhileUnsubscribed.WaitAsync(Timeout, cancellationToken);
            }
        }, cancellationToken);

        Task invoker = Task.Run(async () =>
        {
            for (int round = 0; round < RoundCount; round++)
            {
                await subscribed.WaitAsync(Timeout, cancellationToken);
                weakEvent.Invoke(new object(), new TestEventArgs(round * 2));
                invokedWhileSubscribed.Release();

                await unsubscribed.WaitAsync(Timeout, cancellationToken);
                weakEvent.Invoke(new object(), new TestEventArgs((round * 2) + 1));
                invokedWhileUnsubscribed.Release();
            }
        }, cancellationToken);

        await Task.WhenAll(modifier, invoker).WaitAsync(Timeout, cancellationToken);

        Assert.AreEqual(RoundCount * 2, stableTarget.CallCount);
        Assert.AreEqual(RoundCount, changingTarget.TotalCallCount);
        Assert.AreEqual(0, changingTarget.DuplicateCallCount);
        for (int round = 0; round < RoundCount; round++)
        {
            Assert.AreEqual(1, changingTarget.GetCallCount(round * 2));
            Assert.AreEqual(0, changingTarget.GetCallCount((round * 2) + 1));
        }

        GC.KeepAlive(stableTarget);
        GC.KeepAlive(changingTarget);
    }

    [TestMethod]
    public async Task ConcurrentHandlerChurnAndInvoke_PreservesInvocationConsistencyAndFinalState()
    {
        const int OperationCount = 2_048;
        const int FinalInvocation = int.MaxValue;
        WeakEvent<object, TestEventArgs> weakEvent = new();
        CountingTarget stableTarget = new();
        InvocationRecorder changingTarget = new();
        EventHandler<object, TestEventArgs> changingHandler = changingTarget.Handle;
        CancellationToken cancellationToken = TestContext.CancellationToken;
        weakEvent.Subscribe(stableTarget.Handle);
        weakEvent.Subscribe(changingHandler);

        using ManualResetEventSlim start = new(false);
        using CountdownEvent ready = new(2);
        Task modifier = Task.Run(() =>
        {
            ready.Signal();
            start.Wait(cancellationToken);
            for (int operation = 0; operation < OperationCount; operation++)
            {
                weakEvent.Unsubscribe(changingHandler);
                weakEvent.Subscribe(changingHandler);
            }
        }, cancellationToken);
        Task invoker = Task.Run(() =>
        {
            ready.Signal();
            start.Wait(cancellationToken);
            for (int invocation = 0; invocation < OperationCount; invocation++)
            {
                weakEvent.Invoke(new object(), new TestEventArgs(invocation));
            }
        }, cancellationToken);

        bool workersReady = ready.Wait(Timeout, cancellationToken);
        start.Set();
        Assert.IsTrue(workersReady);
        await Task.WhenAll(modifier, invoker).WaitAsync(Timeout, cancellationToken);

        weakEvent.Invoke(new object(), new TestEventArgs(FinalInvocation));

        Assert.AreEqual(OperationCount + 1, stableTarget.CallCount);
        Assert.AreEqual(0, changingTarget.DuplicateCallCount);
        Assert.AreEqual(1, changingTarget.GetCallCount(FinalInvocation));
        Assert.IsInRange(1, OperationCount + 1, changingTarget.TotalCallCount);
        GC.KeepAlive(stableTarget);
        GC.KeepAlive(changingTarget);
    }

    private static CountingTarget[] CreateTargets(int count)
    {
        CountingTarget[] targets = new CountingTarget[count];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = new CountingTarget();
        }

        return targets;
    }

    private static EventHandler<object, TestEventArgs>[] CreateHandlers(
        CountingTarget[] targets)
    {
        EventHandler<object, TestEventArgs>[] handlers =
            new EventHandler<object, TestEventArgs>[targets.Length];
        for (int i = 0; i < handlers.Length; i++)
        {
            handlers[i] = targets[i].Handle;
        }

        return handlers;
    }

    private static void AssertAllCounts(CountingTarget[] targets, int expected)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            Assert.AreEqual(
                expected,
                targets[i].CallCount,
                $"Unexpected call count for handler {i}.");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference SubscribeTemporaryCountingTarget(
        WeakEvent<object, TestEventArgs> weakEvent)
    {
        CountingTarget target = new();
        weakEvent.Subscribe(target.Handle);
        return new WeakReference(target);
    }

    private sealed class InvocationRecorder
    {
        private readonly ConcurrentDictionary<int, byte> _invocations = new();
        private int _duplicateCallCount;
        private int _totalCallCount;

        public int DuplicateCallCount => Volatile.Read(ref _duplicateCallCount);

        public int TotalCallCount => Volatile.Read(ref _totalCallCount);

        public int GetCallCount(int invocation)
        {
            return _invocations.ContainsKey(invocation) ? 1 : 0;
        }

        public void Handle(object sender, TestEventArgs args)
        {
            _ = sender;
            Interlocked.Increment(ref _totalCallCount);
            if (!_invocations.TryAdd(args.Value, 0))
            {
                Interlocked.Increment(ref _duplicateCallCount);
            }
        }
    }
}
