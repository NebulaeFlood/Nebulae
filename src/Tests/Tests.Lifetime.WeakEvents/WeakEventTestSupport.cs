namespace Tests.Lifetime.WeakEvents;

internal sealed class TestEventArgs(int value)
{
    public int Value { get; } = value;
}

internal sealed class Recorder
{
    public List<string> Entries { get; } = [];
}

internal sealed class RecordingTarget(string name)
{
    public void Record(Recorder sender, TestEventArgs args)
    {
        sender.Entries.Add($"{name}:{args.Value}");
    }

    public void RecordFirst(Recorder sender, TestEventArgs args)
    {
        sender.Entries.Add($"{name}-first:{args.Value}");
    }

    public void RecordSecond(Recorder sender, TestEventArgs args)
    {
        sender.Entries.Add($"{name}-second:{args.Value}");
    }

    public void RecordThird(Recorder sender, TestEventArgs args)
    {
        sender.Entries.Add($"{name}-third:{args.Value}");
    }

    public void RecordContravariant(object sender, object args)
    {
        ((Recorder)sender).Entries.Add(
            $"{name}:{((TestEventArgs)args).Value}");
    }
}

internal sealed class CountingTarget
{
    private int _callCount;

    public int CallCount => Volatile.Read(ref _callCount);

    public void Handle(object sender, TestEventArgs args)
    {
        _ = sender;
        _ = args;
        Interlocked.Increment(ref _callCount);
    }
}

internal static class WeakEventTestSupport
{
    public static void RecordStatic(Recorder sender, TestEventArgs args)
    {
        sender.Entries.Add($"static:{args.Value}");
    }

    public static void RecordContravariantStatic(object sender, object args)
    {
        ((Recorder)sender).Entries.Add(
            $"contravariant-static:{((TestEventArgs)args).Value}");
    }
}
