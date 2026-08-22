using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Lifetime.WeakEvents;
using System.Runtime.CompilerServices;

namespace Tests.Lifetime.WeakEvents;

[TestClass]
public sealed class WeakEventLifetimeTests
{
    [TestMethod]
    public void CollectedInstanceSubscriber_IsNotRootedOrInvoked()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        weakEvent.Subscribe(WeakEventTestSupport.RecordStatic);
        WeakReference subscriber = SubscribeTemporaryTarget(weakEvent, "collected");

        CollectUntilDead(subscriber);
        weakEvent.Invoke(sender, new TestEventArgs(29));

        Assert.IsFalse(subscriber.IsAlive);
        Assert.AreEqual("static:29", string.Join('|', sender.Entries));
    }

    [TestMethod]
    public void Purge_AfterSubscriberCollection_PreservesLiveHandlersAndRemainsReusable()
    {
        WeakEvent<Recorder, TestEventArgs> weakEvent = new();
        Recorder sender = new();
        RecordingTarget first = new("first");
        RecordingTarget second = new("second");
        RecordingTarget afterPurge = new("after");
        weakEvent.Subscribe(first.Record);
        WeakReference collected = SubscribeTemporaryTarget(weakEvent, "collected");
        weakEvent.Subscribe(second.Record);

        CollectUntilDead(collected);
        weakEvent.Purge();
        weakEvent.Subscribe(afterPurge.Record);
        weakEvent.Invoke(sender, new TestEventArgs(31));

        Assert.IsFalse(collected.IsAlive);
        Assert.AreEqual(
            "first:31|second:31|after:31",
            string.Join('|', sender.Entries));
        GC.KeepAlive(first);
        GC.KeepAlive(second);
        GC.KeepAlive(afterPurge);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static WeakReference SubscribeTemporaryTarget(
        WeakEvent<Recorder, TestEventArgs> weakEvent,
        string name)
    {
        RecordingTarget target = new(name);
        weakEvent.Subscribe(target.Record);
        return new WeakReference(target);
    }

    internal static void CollectUntilDead(params WeakReference[] references)
    {
        for (int attempt = 0;
             attempt < 10 && Array.Exists(references, static reference => reference.IsAlive);
             attempt++)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
        }
    }
}
