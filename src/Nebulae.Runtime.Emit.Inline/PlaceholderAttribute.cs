using System;

namespace Nebulae.Runtime.Emit.Inline
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class PlaceholderAttribute(PlaceholderCode code, PlaceholderOperand operand = PlaceholderOperand.None, bool isPrefix = false, bool isPrimitive = true) : Attribute
    {
        public readonly PlaceholderCode Code = code;

        public readonly PlaceholderOperand Operand = operand;

        public readonly bool IsPrefix = isPrefix;

        public readonly bool IsPrimitive = isPrimitive;
    }
}
