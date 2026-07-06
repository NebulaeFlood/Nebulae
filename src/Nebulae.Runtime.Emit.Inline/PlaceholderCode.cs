namespace Nebulae.Runtime.Emit.Inline
{
    internal enum PlaceholderCode
    {
        // Basic
        Nop,
        Break,

        // Extension
        Label,
        Ldtype,
        Fail,

        // Arguments
        Ldarg,
        Ldarga,
        Starg,

        // Locals
        Ldloc,
        Ldloca,
        Stloc,

        // Constants
        Ldnull,
        Ldc_I4,
        Ldc_I8,
        Ldc_R4,
        Ldc_R8,
        Ldstr,

        // Stack
        Dup,
        Pop,

        // Fields
        Ldfld,
        Ldflda,
        Stfld,
        Ldsfld,
        Ldsflda,
        Stsfld,

        // Arrays
        Newarr,
        Ldlen,
        Ldelem,
        Ldelem_I,
        Ldelem_I1,
        Ldelem_I2,
        Ldelem_I4,
        Ldelem_I8,
        Ldelem_U1,
        Ldelem_U2,
        Ldelem_U4,
        Ldelem_R4,
        Ldelem_R8,
        Ldelem_Ref,
        Ldelema,
        Stelem,
        Stelem_I,
        Stelem_I1,
        Stelem_I2,
        Stelem_I4,
        Stelem_I8,
        Stelem_R4,
        Stelem_R8,
        Stelem_Ref,

        // Indirect and object memory
        Ldind_I,
        Ldind_I1,
        Ldind_I2,
        Ldind_I4,
        Ldind_I8,
        Ldind_U1,
        Ldind_U2,
        Ldind_U4,
        Ldind_R4,
        Ldind_R8,
        Ldind_Ref,
        Stind_I,
        Stind_I1,
        Stind_I2,
        Stind_I4,
        Stind_I8,
        Stind_R4,
        Stind_R8,
        Stind_Ref,
        Ldobj,
        Stobj,
        Cpobj,
        Initobj,

        // Calls
        Call,
        Callvirt,
        Calli,
        Newobj,
        Jmp,
        Ldftn,
        Ldvirtftn,
        Ret,

        // Branches
        Br,
        Brfalse,
        Brtrue,
        Beq,
        Bne_Un,
        Bge,
        Bge_Un,
        Bgt,
        Bgt_Un,
        Ble,
        Ble_Un,
        Blt,
        Blt_Un,
        Switch,
        Leave,

        // Comparisons
        Ceq,
        Cgt,
        Cgt_Un,
        Clt,
        Clt_Un,

        // Arithmetic
        Add,
        Add_Ovf,
        Add_Ovf_Un,
        Sub,
        Sub_Ovf,
        Sub_Ovf_Un,
        Mul,
        Mul_Ovf,
        Mul_Ovf_Un,
        Div,
        Div_Un,
        Rem,
        Rem_Un,
        Neg,

        // Bitwise
        And,
        Or,
        Xor,
        Not,
        Shl,
        Shr,
        Shr_Un,

        // Conversions
        Conv_I,
        Conv_I1,
        Conv_I2,
        Conv_I4,
        Conv_I8,
        Conv_U,
        Conv_U1,
        Conv_U2,
        Conv_U4,
        Conv_U8,
        Conv_R_Un,
        Conv_R4,
        Conv_R8,

        Conv_Ovf_I,
        Conv_Ovf_I1,
        Conv_Ovf_I2,
        Conv_Ovf_I4,
        Conv_Ovf_I8,
        Conv_Ovf_U,
        Conv_Ovf_U1,
        Conv_Ovf_U2,
        Conv_Ovf_U4,
        Conv_Ovf_U8,

        Conv_Ovf_I_Un,
        Conv_Ovf_I1_Un,
        Conv_Ovf_I2_Un,
        Conv_Ovf_I4_Un,
        Conv_Ovf_I8_Un,
        Conv_Ovf_U_Un,
        Conv_Ovf_U1_Un,
        Conv_Ovf_U2_Un,
        Conv_Ovf_U4_Un,
        Conv_Ovf_U8_Un,
        Ckfinite,

        // Types and objects
        Box,
        Unbox,
        Unbox_Any,
        Castclass,
        Isinst,
        Ldtoken,
        Sizeof,

        // Typed references
        Mkrefany,
        Refanyval,
        Refanytype,

        // Raw memory
        Localloc,
        Cpblk,
        Initblk,

        // Exceptions
        Throw,
        Rethrow,
        Endfinally,
        Endfilter,

        // Prefixes
        Unaligned,
        Volatile,
        Tail,
        Constrained,
        Readonly,
        No,

        // Advanced
        Arglist
    }
}
