namespace Tests.Reflection.Fixtures;

internal class ReflectionTarget
{
    public ReflectionTarget(int value)
    {
        Value = value;
        _fieldValue = value;
        HiddenValue = value;
        Label = "public";
    }

    private ReflectionTarget(int value, string label)
    {
        Value = value;
        _fieldValue = value;
        HiddenValue = value;
        Label = label;
    }

    public int Value { get; set; }

    public string Label { get; }

    public static int StaticValue { get; set; }

    public static int StaticWriteOnlyValue
    {
        set => StaticValue = value;
    }

    public string this[int index] => $"{Label}:{index}";

    public int this[string value] => value.Length;

    private string this[long index] => $"hidden:{index}";

    public event EventHandler? Changed;

    public static event EventHandler? StaticChanged
    {
        add { }
        remove { }
    }

    private int HiddenValue { get; set; }

    private event EventHandler? HiddenChanged
    {
        add => _hiddenChanged += value;
        remove => _hiddenChanged -= value;
    }

    public int Offset(int value) => Value + value;

    public virtual int VirtualOffset(int value) => Value + value;

    public int TextLength(string value) => Value + value.Length;

    public static int Multiply(int left, int right) => left * right;

    public static void Increment(ref int value) => value++;

    public void RaiseHiddenChanged() => _hiddenChanged?.Invoke(this, EventArgs.Empty);

    public int GetFieldValue() => _fieldValue;

    public static int GetStaticFieldValue() => _staticFieldValue;

    private string Describe() => $"{Label}:none";

    private string Describe(int value) => $"{Label}:number:{value}";

    private string Describe(string value) => $"{Label}:text:{value}";

    private EventHandler? _hiddenChanged;

    private int _fieldValue;

    private static int _staticFieldValue = 7;
}

internal sealed class DerivedReflectionTarget(int value) : ReflectionTarget(value)
{
    public override int VirtualOffset(int argument) => (Value * 10) + argument;
}

internal class IndexerBase
{
    public string this[int index] => $"base:{index}";

    private string this[string index] => $"private:{index}";
}

internal sealed class IndexerDerived : IndexerBase
{
    public string this[Guid index] => $"derived:{index}";
}

internal sealed class DefaultBinderTarget(long value)
{
    public string this[long index] => $"item:{value + index}";

    public long Accept(long argument) => value + argument;
}
