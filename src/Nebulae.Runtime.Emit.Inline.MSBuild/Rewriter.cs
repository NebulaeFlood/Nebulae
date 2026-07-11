using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    internal static class Rewriter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Consume(this Instruction instruction)
        {
            instruction.OpCode = OpCodes.Nop;
            instruction.Operand = Obsolete;
        }

        public static void Rewrite(MethodDefinition definition)
        {
            try
            {
                var context = RewriteContext.Create(definition);
                var placeholders = context.Placeholers;

                for (int i = 0; i < placeholders.Length; i++)
                {
                    ref Placeholder placeholder = ref placeholders[i];
                    Instruction instruction = placeholder.Instruction;

                    if (placeholder.IsPrimitive)
                    {
                        instruction.OpCode = OpCodeMaps[placeholder.Code];
                        instruction.Operand = placeholder.Operand.Resovle(instruction, context);

                        if (placeholder.IsPrefix)
                        {
                            instruction.TrimEnd();
                        }
                    }
                    else
                    {
                        switch (placeholder.Code)
                        {
                            case PlaceholderCode.Ldtype:
                                instruction.Consume();
                                break;
                            case PlaceholderCode.Fail:
                                for (var current = instruction.Previous; current?.OpCode.Code is Code.Nop; current = current.Previous)
                                {
                                    current.Consume();
                                }

                                instruction.Consume();
                                instruction = instruction.Next;

                                if (instruction?.OpCode.Code is not Code.Throw)
                                {
                                    throw new InvalidProgramException(
                                        $"Cannot resolve IL.Fail, the instruction sequence is incompatible.")
                                        .With(placeholder.Instruction);
                                }

                                do
                                {
                                    instruction.Consume();
                                    instruction = instruction.Next;
                                }
                                while (instruction is not null);
                                break;
                            default:
                                throw new NotSupportedException($"Unsupported placeholder code: {placeholder.Code}.")
                                    .With(instruction);
                        }
                    }
                }

                definition.Finialize(context);
            }
            catch (Exception e)
            {
                if (!e.TryGetInstructionOffset(out int offset))
                {
                    throw new InvalidProgramException(
                        $"Cannot inline IL code for method '{definition.FullName}'.", e);
                }

                if (!definition.TryGetSequencePoint(offset, out SequencePoint point))
                {
                    throw new InvalidProgramException(
                        $"Cannot inline IL code for method '{definition.FullName}' " +
                        $"at offset '{offset:X4}'.", e);
                }

                throw new InvalidProgramException(
                    $"Cannot inline IL code for method '{definition.FullName}' " +
                    $"at offset '{offset:X4}'.", e).With(point);
            }
        }


        private static object? Resovle(this PlaceholderOperand type, Instruction instruction, in RewriteContext context)
        {
            return type switch
            {
                PlaceholderOperand.None => null,
                PlaceholderOperand.Argument => instruction.ResolveParameter(context),
                PlaceholderOperand.Variable => instruction.ResolveVariable(context),
                PlaceholderOperand.Byte => instruction.ResolveByte(),
                PlaceholderOperand.Int32 => instruction.ResolveInt32(),
                PlaceholderOperand.Int64 => instruction.ResolveInt64(),
                PlaceholderOperand.Single => instruction.ResolveSingle(),
                PlaceholderOperand.Double => instruction.ResolveDouble(),
                PlaceholderOperand.String => instruction.ResolveString(),
                PlaceholderOperand.Branch => instruction.ResolveBranch(context),
                PlaceholderOperand.Branches => instruction.ResolveBranches(context),
                PlaceholderOperand.TypeRef => instruction.ResolveTypeReference(),
                PlaceholderOperand.FieldRef => instruction.ResolveFieldReference(context),
                PlaceholderOperand.MethodRef => instruction.ResolveMethodReference(context),
                PlaceholderOperand.Signature => instruction.ResolveMethodReference(context).MakeCallSite(),
                _ => throw new InvalidProgramException(
                    $"Invalid placeholder operand type '{type}'.")
                    .With(instruction)
            };
        }

        private static void Finialize(this MethodDefinition definition, in RewriteContext context)
        {
            var body = context.MethodBody;
            var instructions = context.Instructions;

            bool anyBranch = false;

            for (int i = instructions.Count - 1; i >= 0; i--)
            {
                var instruction = instructions[i];
                var operand = instruction.Operand;

                if (operand == Obsolete)
                {
                    context.Remove(instruction);
                    instructions.RemoveAt(i);
                    continue;
                }

                switch (instruction.OpCode.Code)
                {
                    case Code.Ldarg:
                        int index = ((ParameterDefinition)operand).Index;

                        if (index is -1 && operand == body.ThisParameter)
                        {
                            index = 0;
                        }
                        else if (definition.HasThis)
                        {
                            index++;
                        }

                        switch (index)
                        {
                            case 0:
                                instruction.OpCode = OpCodes.Ldarg_0;
                                instruction.Operand = null;
                                break;
                            case 1:
                                instruction.OpCode = OpCodes.Ldarg_1;
                                instruction.Operand = null;
                                break;
                            case 2:
                                instruction.OpCode = OpCodes.Ldarg_2;
                                instruction.Operand = null;
                                break;
                            case 3:
                                instruction.OpCode = OpCodes.Ldarg_3;
                                instruction.Operand = null;
                                break;
                            default:
                                if (index <= byte.MaxValue)
                                {
                                    instruction.OpCode = OpCodes.Ldarg_S;
                                }
                                break;
                        }
                        continue;
                    case Code.Ldarga:
                        index = ((ParameterDefinition)operand).Index;

                        if (index is -1 && operand == body.ThisParameter)
                        {
                            index = 0;
                        }
                        else if (definition.HasThis)
                        {
                            index++;
                        }

                        if (index <= byte.MaxValue)
                        {
                            instruction.OpCode = OpCodes.Ldarga_S;
                        }
                        continue;
                    case Code.Ldloc:
                        index = ((VariableDefinition)operand).Index;

                        switch (index)
                        {
                            case 0:
                                instruction.OpCode = OpCodes.Ldloc_0;
                                instruction.Operand = null;
                                break;
                            case 1:
                                instruction.OpCode = OpCodes.Ldloc_1;
                                instruction.Operand = null;
                                break;
                            case 2:
                                instruction.OpCode = OpCodes.Ldloc_2;
                                instruction.Operand = null;
                                break;
                            case 3:
                                instruction.OpCode = OpCodes.Ldloc_3;
                                instruction.Operand = null;
                                break;
                            default:
                                if (index <= byte.MaxValue)
                                {
                                    instruction.OpCode = OpCodes.Ldloc_S;
                                }
                                break;
                        }
                        continue;
                    case Code.Ldloca:
                        index = ((VariableDefinition)operand).Index;

                        if (index <= byte.MaxValue)
                        {
                            instruction.OpCode = OpCodes.Ldloca_S;
                        }
                        continue;
                    case Code.Starg:
                        index = ((ParameterDefinition)operand).Index;

                        if (index is -1 && operand == body.ThisParameter)
                        {
                            index = 0;
                        }
                        else if (definition.HasThis)
                        {
                            index++;
                        }

                        if (index <= byte.MaxValue)
                        {
                            instruction.OpCode = OpCodes.Starg_S;
                        }
                        continue;
                    case Code.Stloc:
                        index = ((VariableDefinition)operand).Index;

                        switch (index)
                        {
                            case 0:
                                instruction.OpCode = OpCodes.Stloc_0;
                                instruction.Operand = null;
                                break;
                            case 1:
                                instruction.OpCode = OpCodes.Stloc_1;
                                instruction.Operand = null;
                                break;
                            case 2:
                                instruction.OpCode = OpCodes.Stloc_2;
                                instruction.Operand = null;
                                break;
                            case 3:
                                instruction.OpCode = OpCodes.Stloc_3;
                                instruction.Operand = null;
                                break;
                            default:
                                if (index <= byte.MaxValue)
                                {
                                    instruction.OpCode = OpCodes.Stloc_S;
                                }
                                break;
                        }
                        continue;
                    case Code.Ldelem_Any:
                        switch (((TypeReference)instruction.Operand).MetadataType)
                        {
                            case MetadataType.SByte:
                                instruction.OpCode = OpCodes.Ldelem_I1;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Byte:
                            case MetadataType.Boolean:
                                instruction.OpCode = OpCodes.Ldelem_U1;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int16:
                                instruction.OpCode = OpCodes.Ldelem_I2;
                                instruction.Operand = null;
                                break;
                            case MetadataType.UInt16:
                            case MetadataType.Char:
                                instruction.OpCode = OpCodes.Ldelem_U2;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int32:
                                instruction.OpCode = OpCodes.Ldelem_I4;
                                instruction.Operand = null;
                                break;
                            case MetadataType.UInt32:
                                instruction.OpCode = OpCodes.Ldelem_U4;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int64:
                            case MetadataType.UInt64:
                                instruction.OpCode = OpCodes.Ldelem_I8;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Single:
                                instruction.OpCode = OpCodes.Ldelem_R4;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Double:
                                instruction.OpCode = OpCodes.Ldelem_R8;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Array:
                            case MetadataType.Class:
                            case MetadataType.Object:
                            case MetadataType.String:
                                instruction.OpCode = OpCodes.Ldelem_Ref;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Pointer:
                            case MetadataType.IntPtr:
                            case MetadataType.UIntPtr:
                            case MetadataType.FunctionPointer:
                                instruction.OpCode = OpCodes.Ldelem_I;
                                instruction.Operand = null;
                                break;
                            default:
                                break;
                        }
                        continue;
                    case Code.Stelem_Any:
                        switch (((TypeReference)instruction.Operand).MetadataType)
                        {
                            case MetadataType.SByte:
                                instruction.OpCode = OpCodes.Stelem_I1;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Byte:
                            case MetadataType.Boolean:
                                instruction.OpCode = OpCodes.Stelem_I1;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int16:
                                instruction.OpCode = OpCodes.Stelem_I2;
                                instruction.Operand = null;
                                break;
                            case MetadataType.UInt16:
                            case MetadataType.Char:
                                instruction.OpCode = OpCodes.Stelem_I2;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int32:
                            case MetadataType.UInt32:
                                instruction.OpCode = OpCodes.Stelem_I4;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Int64:
                            case MetadataType.UInt64:
                                instruction.OpCode = OpCodes.Stelem_I8;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Single:
                                instruction.OpCode = OpCodes.Stelem_R4;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Double:
                                instruction.OpCode = OpCodes.Stelem_R8;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Array:
                            case MetadataType.Class:
                            case MetadataType.Object:
                            case MetadataType.String:
                                instruction.OpCode = OpCodes.Stelem_Ref;
                                instruction.Operand = null;
                                break;
                            case MetadataType.Pointer:
                            case MetadataType.IntPtr:
                            case MetadataType.UIntPtr:
                            case MetadataType.FunctionPointer:
                                instruction.OpCode = OpCodes.Stelem_I;
                                instruction.Operand = null;
                                break;
                            default:
                                break;
                        }
                        continue;
                    case Code.Ldc_I4:
                        int value = (int)operand;

                        switch (value)
                        {
                            case -1:
                                instruction.OpCode = OpCodes.Ldc_I4_M1;
                                instruction.Operand = null;
                                break;
                            case 0:
                                instruction.OpCode = OpCodes.Ldc_I4_0;
                                instruction.Operand = null;
                                break;
                            case 1:
                                instruction.OpCode = OpCodes.Ldc_I4_1;
                                instruction.Operand = null;
                                break;
                            case 2:
                                instruction.OpCode = OpCodes.Ldc_I4_2;
                                instruction.Operand = null;
                                break;
                            case 3:
                                instruction.OpCode = OpCodes.Ldc_I4_3;
                                instruction.Operand = null;
                                break;
                            case 4:
                                instruction.OpCode = OpCodes.Ldc_I4_4;
                                instruction.Operand = null;
                                break;
                            case 5:
                                instruction.OpCode = OpCodes.Ldc_I4_5;
                                instruction.Operand = null;
                                break;
                            case 6:
                                instruction.OpCode = OpCodes.Ldc_I4_6;
                                instruction.Operand = null;
                                break;
                            case 7:
                                instruction.OpCode = OpCodes.Ldc_I4_7;
                                instruction.Operand = null;
                                break;
                            case 8:
                                instruction.OpCode = OpCodes.Ldc_I4_8;
                                instruction.Operand = null;
                                break;
                            default:
                                if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
                                {
                                    instruction.OpCode = OpCodes.Ldc_I4_S;
                                    instruction.Operand = (sbyte)value;
                                }
                                break;
                        }
                        continue;
                    case Code.Tail:
                        if (i + 2 >= instructions.Count)
                        {
                            throw new InvalidProgramException(
                                $"Invalid IL code, the 'tail.' prefix must be followed by a call/calli/callvirt instruction.")
                                .With(instruction);
                        }

                        instruction = instructions[i + 1];

                        if (instruction.OpCode.Code is not Code.Call and not Code.Calli and not Code.Callvirt)
                        {
                            throw new InvalidProgramException(
                                $"Invalid IL code, the 'tail.' prefix must be followed by a call/calli/callvirt instruction.")
                                .With(instruction);
                        }

                        instruction = instructions[i + 2];

                        if (instruction.OpCode.Code is Code.Nop)
                        {
                            context.Remove(instruction);
                            instructions.RemoveAt(i + 2);
                        }
                        continue;
                }

                if (!anyBranch && instruction.OpCode.OperandType is OperandType.InlineBrTarget)
                {
                    anyBranch = true;
                }
            }

            if (!anyBranch)
            {
                return;
            }

        Retry:
            bool changed = false;
            instructions.Measure();

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (instruction.OpCode.OperandType is not OperandType.InlineBrTarget)
                {
                    continue;
                }

                var opCode = instruction.OpCode;
                // target - (start + op_size)
                var offset = ((Instruction)instruction.Operand).Offset - (instruction.Offset + opCode.Size);

                if (offset - 1 >= sbyte.MinValue && offset - 4 <= sbyte.MaxValue)
                {
                    instruction.OpCode = opCode.Code switch
                    {
                        Code.Br => OpCodes.Br_S,
                        Code.Brfalse => OpCodes.Brfalse_S,
                        Code.Brtrue => OpCodes.Brtrue_S,
                        Code.Beq => OpCodes.Beq_S,
                        Code.Bge => OpCodes.Bge_S,
                        Code.Bgt => OpCodes.Bgt_S,
                        Code.Ble => OpCodes.Ble_S,
                        Code.Blt => OpCodes.Blt_S,
                        Code.Bne_Un => OpCodes.Bne_Un_S,
                        Code.Bge_Un => OpCodes.Bge_Un_S,
                        Code.Bgt_Un => OpCodes.Bgt_Un_S,
                        Code.Ble_Un => OpCodes.Ble_Un_S,
                        Code.Blt_Un => OpCodes.Blt_Un_S,
                        Code.Leave => OpCodes.Leave_S,
                        _ => opCode
                    };

                    changed = true;
                }
            }

            if (changed)
            {
                goto Retry;
            }
        }

        private static void Measure(this Collection<Instruction> instructions)
        {
            int offset = 0;

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                instruction.Offset = offset;
                offset += instruction.GetSize();
            }
        }

        private static void TrimEnd(this Instruction instruction)
        {
            for (var current = instruction.Next; current?.OpCode.Code is Code.Nop; current = current.Next)
            {
                current.Consume();
            }
        }


        //------------------------------------------------------
        //
        //  Private Static Fields
        //
        //------------------------------------------------------

        #region Private Static Fields

        private static readonly object Obsolete = new();
        private static readonly Dictionary<PlaceholderCode, OpCode> OpCodeMaps = new()
        {
            // Basic
            [PlaceholderCode.Nop] = OpCodes.Nop,
            [PlaceholderCode.Break] = OpCodes.Break,

            // Arguments
            [PlaceholderCode.Ldarg] = OpCodes.Ldarg,
            [PlaceholderCode.Ldarga] = OpCodes.Ldarga,
            [PlaceholderCode.Starg] = OpCodes.Starg,

            // Locals
            [PlaceholderCode.Ldloc] = OpCodes.Ldloc,
            [PlaceholderCode.Ldloca] = OpCodes.Ldloca,
            [PlaceholderCode.Stloc] = OpCodes.Stloc,

            // Constants
            [PlaceholderCode.Ldnull] = OpCodes.Ldnull,
            [PlaceholderCode.Ldc_I4] = OpCodes.Ldc_I4,
            [PlaceholderCode.Ldc_I8] = OpCodes.Ldc_I8,
            [PlaceholderCode.Ldc_R4] = OpCodes.Ldc_R4,
            [PlaceholderCode.Ldc_R8] = OpCodes.Ldc_R8,
            [PlaceholderCode.Ldstr] = OpCodes.Ldstr,

            // Stack
            [PlaceholderCode.Dup] = OpCodes.Dup,
            [PlaceholderCode.Pop] = OpCodes.Pop,

            // Fields
            [PlaceholderCode.Ldfld] = OpCodes.Ldfld,
            [PlaceholderCode.Ldflda] = OpCodes.Ldflda,
            [PlaceholderCode.Stfld] = OpCodes.Stfld,
            [PlaceholderCode.Ldsfld] = OpCodes.Ldsfld,
            [PlaceholderCode.Ldsflda] = OpCodes.Ldsflda,
            [PlaceholderCode.Stsfld] = OpCodes.Stsfld,

            // Arrays
            [PlaceholderCode.Newarr] = OpCodes.Newarr,
            [PlaceholderCode.Ldlen] = OpCodes.Ldlen,
            [PlaceholderCode.Ldelem] = OpCodes.Ldelem_Any,
            [PlaceholderCode.Ldelem_I] = OpCodes.Ldelem_I,
            [PlaceholderCode.Ldelem_I1] = OpCodes.Ldelem_I1,
            [PlaceholderCode.Ldelem_I2] = OpCodes.Ldelem_I2,
            [PlaceholderCode.Ldelem_I4] = OpCodes.Ldelem_I4,
            [PlaceholderCode.Ldelem_I8] = OpCodes.Ldelem_I8,
            [PlaceholderCode.Ldelem_U1] = OpCodes.Ldelem_U1,
            [PlaceholderCode.Ldelem_U2] = OpCodes.Ldelem_U2,
            [PlaceholderCode.Ldelem_U4] = OpCodes.Ldelem_U4,
            [PlaceholderCode.Ldelem_R4] = OpCodes.Ldelem_R4,
            [PlaceholderCode.Ldelem_R8] = OpCodes.Ldelem_R8,
            [PlaceholderCode.Ldelem_Ref] = OpCodes.Ldelem_Ref,
            [PlaceholderCode.Ldelema] = OpCodes.Ldelema,
            [PlaceholderCode.Stelem] = OpCodes.Stelem_Any,
            [PlaceholderCode.Stelem_I] = OpCodes.Stelem_I,
            [PlaceholderCode.Stelem_I1] = OpCodes.Stelem_I1,
            [PlaceholderCode.Stelem_I2] = OpCodes.Stelem_I2,
            [PlaceholderCode.Stelem_I4] = OpCodes.Stelem_I4,
            [PlaceholderCode.Stelem_I8] = OpCodes.Stelem_I8,
            [PlaceholderCode.Stelem_R4] = OpCodes.Stelem_R4,
            [PlaceholderCode.Stelem_R8] = OpCodes.Stelem_R8,
            [PlaceholderCode.Stelem_Ref] = OpCodes.Stelem_Ref,

            // Indirect and object memory
            [PlaceholderCode.Ldind_I] = OpCodes.Ldind_I,
            [PlaceholderCode.Ldind_I1] = OpCodes.Ldind_I1,
            [PlaceholderCode.Ldind_I2] = OpCodes.Ldind_I2,
            [PlaceholderCode.Ldind_I4] = OpCodes.Ldind_I4,
            [PlaceholderCode.Ldind_I8] = OpCodes.Ldind_I8,
            [PlaceholderCode.Ldind_U1] = OpCodes.Ldind_U1,
            [PlaceholderCode.Ldind_U2] = OpCodes.Ldind_U2,
            [PlaceholderCode.Ldind_U4] = OpCodes.Ldind_U4,
            [PlaceholderCode.Ldind_R4] = OpCodes.Ldind_R4,
            [PlaceholderCode.Ldind_R8] = OpCodes.Ldind_R8,
            [PlaceholderCode.Ldind_Ref] = OpCodes.Ldind_Ref,
            [PlaceholderCode.Stind_I] = OpCodes.Stind_I,
            [PlaceholderCode.Stind_I1] = OpCodes.Stind_I1,
            [PlaceholderCode.Stind_I2] = OpCodes.Stind_I2,
            [PlaceholderCode.Stind_I4] = OpCodes.Stind_I4,
            [PlaceholderCode.Stind_I8] = OpCodes.Stind_I8,
            [PlaceholderCode.Stind_R4] = OpCodes.Stind_R4,
            [PlaceholderCode.Stind_R8] = OpCodes.Stind_R8,
            [PlaceholderCode.Stind_Ref] = OpCodes.Stind_Ref,
            [PlaceholderCode.Ldobj] = OpCodes.Ldobj,
            [PlaceholderCode.Stobj] = OpCodes.Stobj,
            [PlaceholderCode.Cpobj] = OpCodes.Cpobj,
            [PlaceholderCode.Initobj] = OpCodes.Initobj,

            // Calls
            [PlaceholderCode.Call] = OpCodes.Call,
            [PlaceholderCode.Callvirt] = OpCodes.Callvirt,
            [PlaceholderCode.Calli] = OpCodes.Calli,
            [PlaceholderCode.Newobj] = OpCodes.Newobj,
            [PlaceholderCode.Jmp] = OpCodes.Jmp,
            [PlaceholderCode.Ldftn] = OpCodes.Ldftn,
            [PlaceholderCode.Ldvirtftn] = OpCodes.Ldvirtftn,
            [PlaceholderCode.Ret] = OpCodes.Ret,

            // Branches
            [PlaceholderCode.Br] = OpCodes.Br,
            [PlaceholderCode.Brfalse] = OpCodes.Brfalse,
            [PlaceholderCode.Brtrue] = OpCodes.Brtrue,
            [PlaceholderCode.Beq] = OpCodes.Beq,
            [PlaceholderCode.Bne_Un] = OpCodes.Bne_Un,
            [PlaceholderCode.Bge] = OpCodes.Bge,
            [PlaceholderCode.Bge_Un] = OpCodes.Bge_Un,
            [PlaceholderCode.Bgt] = OpCodes.Bgt,
            [PlaceholderCode.Bgt_Un] = OpCodes.Bgt_Un,
            [PlaceholderCode.Ble] = OpCodes.Ble,
            [PlaceholderCode.Ble_Un] = OpCodes.Ble_Un,
            [PlaceholderCode.Blt] = OpCodes.Blt,
            [PlaceholderCode.Blt_Un] = OpCodes.Blt_Un,
            [PlaceholderCode.Switch] = OpCodes.Switch,
            [PlaceholderCode.Leave] = OpCodes.Leave,

            // Comparisons
            [PlaceholderCode.Ceq] = OpCodes.Ceq,
            [PlaceholderCode.Cgt] = OpCodes.Cgt,
            [PlaceholderCode.Cgt_Un] = OpCodes.Cgt_Un,
            [PlaceholderCode.Clt] = OpCodes.Clt,
            [PlaceholderCode.Clt_Un] = OpCodes.Clt_Un,

            // Arithmetic
            [PlaceholderCode.Add] = OpCodes.Add,
            [PlaceholderCode.Add_Ovf] = OpCodes.Add_Ovf,
            [PlaceholderCode.Add_Ovf_Un] = OpCodes.Add_Ovf_Un,
            [PlaceholderCode.Sub] = OpCodes.Sub,
            [PlaceholderCode.Sub_Ovf] = OpCodes.Sub_Ovf,
            [PlaceholderCode.Sub_Ovf_Un] = OpCodes.Sub_Ovf_Un,
            [PlaceholderCode.Mul] = OpCodes.Mul,
            [PlaceholderCode.Mul_Ovf] = OpCodes.Mul_Ovf,
            [PlaceholderCode.Mul_Ovf_Un] = OpCodes.Mul_Ovf_Un,
            [PlaceholderCode.Div] = OpCodes.Div,
            [PlaceholderCode.Div_Un] = OpCodes.Div_Un,
            [PlaceholderCode.Rem] = OpCodes.Rem,
            [PlaceholderCode.Rem_Un] = OpCodes.Rem_Un,
            [PlaceholderCode.Neg] = OpCodes.Neg,

            // Bitwise
            [PlaceholderCode.And] = OpCodes.And,
            [PlaceholderCode.Or] = OpCodes.Or,
            [PlaceholderCode.Xor] = OpCodes.Xor,
            [PlaceholderCode.Not] = OpCodes.Not,
            [PlaceholderCode.Shl] = OpCodes.Shl,
            [PlaceholderCode.Shr] = OpCodes.Shr,
            [PlaceholderCode.Shr_Un] = OpCodes.Shr_Un,

            // Conversions
            [PlaceholderCode.Conv_I] = OpCodes.Conv_I,
            [PlaceholderCode.Conv_I1] = OpCodes.Conv_I1,
            [PlaceholderCode.Conv_I2] = OpCodes.Conv_I2,
            [PlaceholderCode.Conv_I4] = OpCodes.Conv_I4,
            [PlaceholderCode.Conv_I8] = OpCodes.Conv_I8,
            [PlaceholderCode.Conv_U] = OpCodes.Conv_U,
            [PlaceholderCode.Conv_U1] = OpCodes.Conv_U1,
            [PlaceholderCode.Conv_U2] = OpCodes.Conv_U2,
            [PlaceholderCode.Conv_U4] = OpCodes.Conv_U4,
            [PlaceholderCode.Conv_U8] = OpCodes.Conv_U8,
            [PlaceholderCode.Conv_R_Un] = OpCodes.Conv_R_Un,
            [PlaceholderCode.Conv_R4] = OpCodes.Conv_R4,
            [PlaceholderCode.Conv_R8] = OpCodes.Conv_R8,

            [PlaceholderCode.Conv_Ovf_I] = OpCodes.Conv_Ovf_I,
            [PlaceholderCode.Conv_Ovf_I1] = OpCodes.Conv_Ovf_I1,
            [PlaceholderCode.Conv_Ovf_I2] = OpCodes.Conv_Ovf_I2,
            [PlaceholderCode.Conv_Ovf_I4] = OpCodes.Conv_Ovf_I4,
            [PlaceholderCode.Conv_Ovf_I8] = OpCodes.Conv_Ovf_I8,
            [PlaceholderCode.Conv_Ovf_U] = OpCodes.Conv_Ovf_U,
            [PlaceholderCode.Conv_Ovf_U1] = OpCodes.Conv_Ovf_U1,
            [PlaceholderCode.Conv_Ovf_U2] = OpCodes.Conv_Ovf_U2,
            [PlaceholderCode.Conv_Ovf_U4] = OpCodes.Conv_Ovf_U4,
            [PlaceholderCode.Conv_Ovf_U8] = OpCodes.Conv_Ovf_U8,

            [PlaceholderCode.Conv_Ovf_I_Un] = OpCodes.Conv_Ovf_I_Un,
            [PlaceholderCode.Conv_Ovf_I1_Un] = OpCodes.Conv_Ovf_I1_Un,
            [PlaceholderCode.Conv_Ovf_I2_Un] = OpCodes.Conv_Ovf_I2_Un,
            [PlaceholderCode.Conv_Ovf_I4_Un] = OpCodes.Conv_Ovf_I4_Un,
            [PlaceholderCode.Conv_Ovf_I8_Un] = OpCodes.Conv_Ovf_I8_Un,
            [PlaceholderCode.Conv_Ovf_U_Un] = OpCodes.Conv_Ovf_U_Un,
            [PlaceholderCode.Conv_Ovf_U1_Un] = OpCodes.Conv_Ovf_U1_Un,
            [PlaceholderCode.Conv_Ovf_U2_Un] = OpCodes.Conv_Ovf_U2_Un,
            [PlaceholderCode.Conv_Ovf_U4_Un] = OpCodes.Conv_Ovf_U4_Un,
            [PlaceholderCode.Conv_Ovf_U8_Un] = OpCodes.Conv_Ovf_U8_Un,
            [PlaceholderCode.Ckfinite] = OpCodes.Ckfinite,

            // Types and objects
            [PlaceholderCode.Box] = OpCodes.Box,
            [PlaceholderCode.Unbox] = OpCodes.Unbox,
            [PlaceholderCode.Unbox_Any] = OpCodes.Unbox_Any,
            [PlaceholderCode.Castclass] = OpCodes.Castclass,
            [PlaceholderCode.Isinst] = OpCodes.Isinst,
            [PlaceholderCode.Ldtoken] = OpCodes.Ldtoken,
            [PlaceholderCode.Sizeof] = OpCodes.Sizeof,

            // Typed references
            [PlaceholderCode.Mkrefany] = OpCodes.Mkrefany,
            [PlaceholderCode.Refanyval] = OpCodes.Refanyval,
            [PlaceholderCode.Refanytype] = OpCodes.Refanytype,

            // Raw memory
            [PlaceholderCode.Localloc] = OpCodes.Localloc,
            [PlaceholderCode.Cpblk] = OpCodes.Cpblk,
            [PlaceholderCode.Initblk] = OpCodes.Initblk,

            // Exceptions
            [PlaceholderCode.Throw] = OpCodes.Throw,
            [PlaceholderCode.Rethrow] = OpCodes.Rethrow,
            [PlaceholderCode.Endfinally] = OpCodes.Endfinally,
            [PlaceholderCode.Endfilter] = OpCodes.Endfilter,

            // Prefixes
            [PlaceholderCode.Unaligned] = OpCodes.Unaligned,
            [PlaceholderCode.Volatile] = OpCodes.Volatile,
            [PlaceholderCode.Tail] = OpCodes.Tail,
            [PlaceholderCode.Constrained] = OpCodes.Constrained,
            [PlaceholderCode.Readonly] = OpCodes.Readonly,
            [PlaceholderCode.No] = OpCodes.No,

            // Advanced
            [PlaceholderCode.Arglist] = OpCodes.Arglist
        };

        #endregion
    }
}
