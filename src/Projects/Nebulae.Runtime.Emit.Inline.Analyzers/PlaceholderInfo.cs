namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal readonly struct PlaceholderInfo(
        PlaceholderCode code,
        PlaceholderOperand operand,
        bool isPrimitive)
    {
        public readonly PlaceholderCode Code = code;

        public readonly PlaceholderOperand Operand = operand;

        public readonly bool IsPrimitive = isPrimitive;
    }
}
