using Mono.Cecil;
using Mono.Cecil.Cil;
using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    internal readonly struct Placeholder
    {
        private const string PlaceholderAttributeFullName = "Nebulae.Runtime.Emit.Inline.PlaceholderAttribute";


        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        public readonly PlaceholderCode Code;

        public readonly PlaceholderOperand Operand;

        public readonly Instruction Instruction;

        public readonly bool IsPrefix;

        public readonly bool IsPrimitive;

        #endregion


        private Placeholder(PlaceholderCode code, PlaceholderOperand operand, Instruction instruction, bool isPrefix, bool isPrimitive)
        {
            Code = code;
            Operand = operand;
            Instruction = instruction;
            IsPrefix = isPrefix;
            IsPrimitive = isPrimitive;
        }


        public static bool IsPlaceholder(Instruction instruction, out Placeholder placeholer)
        {
            if (instruction.OpCode.Code is not Mono.Cecil.Cil.Code.Call)
            {
                placeholer = default;
                return false;
            }

            if (instruction.Operand is not MethodReference reference)
            {
                placeholer = default;
                return false;
            }

            var definition = reference.Resolve();

            if (definition is null)
            {
                placeholer = default;
                return false;
            }

            var attributes = definition.CustomAttributes;

            for (int i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];

                if (!attribute.AttributeType.FullName.Equals(PlaceholderAttributeFullName, StringComparison.Ordinal))
                {
                    continue;
                }

                placeholer = new Placeholder(
                    (PlaceholderCode)attribute.ConstructorArguments[0].Value,
                    (PlaceholderOperand)attribute.ConstructorArguments[1].Value,
                    instruction,
                    (bool)attribute.ConstructorArguments[2].Value,
                    (bool)attribute.ConstructorArguments[3].Value);

                return true;
            }

            placeholer = default;
            return false;
        }

        public static bool ReferencesPlaceholder(MethodDefinition definition)
        {
            if (!definition.HasBody)
            {
                return false;
            }

            var instructions = definition.Body.Instructions;

            for (int i = 0; i < instructions.Count; i++)
            {
                var instruction = instructions[i];

                if (instruction.OpCode.Code is not Mono.Cecil.Cil.Code.Call)
                {
                    continue;
                }

                if (instruction.Operand is not MethodReference reference)
                {
                    continue;
                }

                definition = reference.Resolve();

                if (definition is null)
                {
                    continue;
                }

                var attributes = definition.CustomAttributes;

                for (int j = 0; j < attributes.Count; j++)
                {
                    var attribute = attributes[j];

                    if (attribute.AttributeType.FullName.Equals(PlaceholderAttributeFullName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
