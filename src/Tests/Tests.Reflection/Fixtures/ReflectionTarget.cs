namespace Tests.Reflection.Fixtures;

internal sealed class ReflectionTarget
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

    public string this[int index] => $"{Label}:{index}";

    public int this[string value] => value.Length;

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

    public int TextLength(string value) => Value + value.Length;

    public static int Multiply(int left, int right) => left * right;

    public void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public int GetFieldValue() => _fieldValue;

    public void SetFieldValue(int value) => _fieldValue = value;

    public static int GetStaticFieldValue() => _staticFieldValue;

    public static void SetStaticFieldValue(int value) => _staticFieldValue = value;

    private string Describe(int value) => $"{Label}:number:{value}";

    private string Describe(string value) => $"{Label}:text:{value}";

    private EventHandler? _hiddenChanged;

    private int _fieldValue;

    private static int _staticFieldValue = 7;
}
