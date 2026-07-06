namespace Nebulae.Runtime.Emit.Inline
{
    internal enum PlaceholderOperand
    {
        None,

        Argument,
        Variable,

        Byte,

        Int32,
        Int64,

        Single,
        Double,

        String,

        Branch,
        Branches,

        TypeRef,
        FieldRef,
        MethodRef,
        Signature
    }
}
