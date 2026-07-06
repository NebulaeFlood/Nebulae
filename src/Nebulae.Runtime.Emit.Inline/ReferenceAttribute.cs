using System;

namespace Nebulae.Runtime.Emit.Inline
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    internal class ReferenceAttribute(ReferenceType type) : Attribute
    {
        public readonly ReferenceType Type = type;
    }
}
