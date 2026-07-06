using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class InstructionHelpers
    {
        public static bool IsCallArrayEmpty(this Instruction instruction)
        {
            return instruction.OpCode.Code is Code.Call
                && instruction.Operand is GenericInstanceMethod method
                && method.Name.Equals(nameof(Array.Empty), StringComparison.Ordinal)
                && method.DeclaringType.FullName.Equals("System.Array", StringComparison.Ordinal);
        }

        public static bool IsCallGetTypeFromHandle(this Instruction instruction)
        {
            return instruction.OpCode.Code is Code.Call
                && instruction.Operand is MethodReference method
                && method.Name.Equals(nameof(Type.GetTypeFromHandle), StringComparison.Ordinal)
                && method.DeclaringType.FullName.Equals("System.Type", StringComparison.Ordinal);
        }

        public static bool IsCallMakeByRefType(this Instruction instruction)
        {
            return instruction.OpCode.Code is Code.Call
                && instruction.Operand is MethodReference method
                && method.Name.Equals(nameof(Type.MakeByRefType), StringComparison.Ordinal)
                && method.DeclaringType.FullName.Equals("System.Type", StringComparison.Ordinal);
        }

        public static bool IsCallRef(this Instruction instruction, out ReferenceType type, out int parameterCount)
        {
            if (instruction.OpCode.Code is not Code.Call and not Code.Callvirt)
            {
                type = default;
                parameterCount = default;
                return false;
            }

            if (instruction.Operand is not MethodReference reference)
            {
                type = default;
                parameterCount = default;
                return false;
            }

            var definition = reference.Resolve();

            if (definition is null)
            {
                type = default;
                parameterCount = default;
                return false;
            }

            var attributes = definition.CustomAttributes;

            for (int i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];

                if (!attribute.AttributeType.FullName.Equals(TypeReferenceHelpers.ReferenceAttributeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                type = (ReferenceType)attribute.ConstructorArguments[0].Value;
                parameterCount = reference.Parameters.Count;
                return true;
            }

            type = default;
            parameterCount = default;
            return false;
        }

        public static bool IsCallRef(this Instruction instruction, ReferenceType type, out int parameterCount)
        {
            if (!instruction.IsCallRef(out var referenceType, out parameterCount))
            {
                return false;
            }

            return referenceType == type;
        }

        public static bool IsCallRef(this Instruction instruction, ReferenceType type)
        {
            if (!instruction.IsCallRef(out var referenceType, out _))
            {
                return false;
            }

            return referenceType == type;
        }
    }
}
