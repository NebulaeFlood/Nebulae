namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal readonly struct PlaceholderInfo(PlaceholderCode code, PlaceholderOperand operand)
    {
        public readonly PlaceholderCode Code = code;

        public readonly PlaceholderOperand Operand = operand;
    }
}
