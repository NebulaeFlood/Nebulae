using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Runtime.CompilerServices;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class OperandHelpers
    {
        //------------------------------------------------------
        //
        //  Public Helpers
        //
        //------------------------------------------------------

        #region Public Helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Instruction AcquirePrevious(this Instruction placeholder, string argumentName)
        {
            return placeholder.Previous
                ?? throw placeholder.Fail(argumentName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Instruction AcquirePrevious(this Instruction instruction, Instruction placeholder, string argumentName)
        {
            return instruction.Previous
                ?? throw placeholder.Fail(argumentName);
        }

        #endregion


        public static ParameterDefinition ResolveParameter(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "argument";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

        Start:
            switch (instruction.OpCode.Code)
            {
                case Code.Ldarg_0:
                    instruction.Consume();
                    return context.Parameters[0];
                case Code.Ldarg_1:
                    instruction.Consume();
                    return context.Parameters[1];
                case Code.Ldarg_2:
                    instruction.Consume();
                    return context.Parameters[2];
                case Code.Ldarg_3:
                    instruction.Consume();
                    return context.Parameters[3];
                case Code.Ldarg_S:
                case Code.Ldarga_S:
                case Code.Ldarg:
                case Code.Ldarga:
                    var parameter = (ParameterDefinition)instruction.Operand;
                    instruction.Consume();
                    return parameter;
                default:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);
                    goto Start;
            }
        }

        public static VariableDefinition ResolveVariable(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "local variable";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

        Start:
            switch (instruction.OpCode.Code)
            {
                case Code.Ldloc_0:
                    instruction.Consume();
                    return context.Variables[0];
                case Code.Ldloc_1:
                    instruction.Consume();
                    return context.Variables[1];
                case Code.Ldloc_2:
                    instruction.Consume();
                    return context.Variables[2];
                case Code.Ldloc_3:
                    instruction.Consume();
                    return context.Variables[3];
                case Code.Ldloc_S:
                case Code.Ldloca_S:
                case Code.Ldloc:
                case Code.Ldloca:
                    var variable = (VariableDefinition)instruction.Operand;
                    instruction.Consume();
                    return variable;
                default:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);
                    goto Start;
            }
        }

        public static byte ResolveByte(this Instruction placeholder)
        {
            const string ArgumentName = "8-bit integer constant";
            return (byte)placeholder
                .AcquirePrevious(ArgumentName)
                .GrabInt32(placeholder, ArgumentName);
        }

        public static int ResolveInt32(this Instruction placeholder)
        {
            const string ArgumentName = "32-bit integer constant";
            return placeholder
                .AcquirePrevious(ArgumentName)
                .GrabInt32(placeholder, ArgumentName);
        }

        public static long ResolveInt64(this Instruction placeholder)
        {
            const string ArgumentName = "64-bit integer constant";
            var instruction = placeholder.AcquirePrevious(placeholder, ArgumentName);

            switch (instruction.OpCode.Code)
            {
                case Code.Ldc_I8:
                    long value = (long)instruction.Operand;
                    instruction.Consume();
                    return value;
                case Code.Conv_I8:
                case Code.Conv_U8:
                    instruction.Consume();
                    instruction.AcquirePrevious(placeholder, ArgumentName);
                    return instruction.GrabInt32(placeholder, ArgumentName);
                default:
                    throw placeholder.Fail(ArgumentName);
            }
        }

        public static float ResolveSingle(this Instruction placeholder)
        {
            const string ArgumentName = "single-precision floating-point constant";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

            if (instruction.OpCode.Code is not Code.Ldc_R4)
            {
                throw placeholder.Fail(ArgumentName);
            }

            float value = (float)instruction.Operand;
            instruction.Consume();
            return value;
        }

        public static double ResolveDouble(this Instruction placeholder)
        {
            const string ArgumentName = "double-precision floating-point constant";

            var instruction = placeholder.AcquirePrevious(ArgumentName);

            if (instruction.OpCode.Code is not Code.Ldc_R8)
            {
                throw placeholder.Fail(ArgumentName);
            }

            double value = (double)instruction.Operand;
            instruction.Consume();
            return value;
        }

        public static string ResolveString(this Instruction placeholder)
        {
            const string ArgumentName = "string constant";
            return placeholder
                .AcquirePrevious(ArgumentName)
                .GrabString(placeholder, ArgumentName);
        }

        public static Instruction ResolveBranch(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "branch label";
            return context.GetLabel(
                placeholder,
                placeholder.AcquirePrevious(ArgumentName)
                    .GrabString(placeholder, ArgumentName));
        }

        public static Instruction[] ResolveBranches(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "branch labels";

            string[] labels = placeholder
                .AcquirePrevious(ArgumentName)
                .GrabStringArray(placeholder, ArgumentName);

            var branches = new Instruction[labels.Length];

            for (int i = 0; i < labels.Length; i++)
            {
                branches[i] = context.GetLabel(placeholder, labels[i]);
            }

            return branches;
        }

        public static TypeReference ResolveTypeReference(this Instruction placeholder)
        {
            const string ArgumentName = "type reference";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

            bool isByRef;

            if (instruction.IsCallMakeByRefType())
            {
                isByRef = true;

                instruction.Consume();
                instruction = instruction.AcquirePrevious(placeholder, ArgumentName);
            }
            else
            {
                isByRef = false;
            }

            if (!instruction.IsCallGetTypeFromHandle())
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            if (instruction.OpCode.Code is not Code.Ldtoken)
            {
                throw placeholder.Fail(ArgumentName);
            }

            var type = (TypeReference)instruction.Operand;
            instruction.Consume();

            return isByRef
                ? type.MakeByReferenceType()
                : type;
        }

        public static FieldReference ResolveFieldReference(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "field reference";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

            if (!instruction.IsCallRef(ReferenceType.Field))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            string fieldName = instruction.GrabString(placeholder, ArgumentName);
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            TypeReference declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
            FieldReference field = declaringType.GetField(fieldName, placeholder) ?? throw new MissingFieldException(
                    $"Cannot find any field named '{fieldName}' in type '{declaringType.FullName}'.")
                    .With(placeholder);

            if (declaringType is GenericInstanceType)
            {
                field = new FieldReference(field.Name, field.FieldType, declaringType);
            }

            return context.Module.ImportReference(field);
        }

        public static MethodReference ResolveMethodReference(this Instruction placeholder, in RewriteContext context)
        {
            const string ArgumentName = "method reference";
            var instruction = placeholder.AcquirePrevious(ArgumentName);

            if (!instruction.IsCallRef(out ReferenceType referenceType, out _))
            {
                throw placeholder.Fail(ArgumentName);
            }

            MethodReference method;

            switch (referenceType)
            {
                case ReferenceType.Constructor:
                    method = instruction.GrabConstructorReference(placeholder);
                    break;
                case ReferenceType.EventAdd:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    EventDefinition @event = instruction.GrabEventReference(placeholder, out TypeReference declaringType);
                    method = @event.AddMethod ?? throw new MissingMethodException(
                        $"Cannot find any add method for event '{@event.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.EventRaise:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    @event = instruction.GrabEventReference(placeholder, out declaringType);
                    method = @event.InvokeMethod ?? throw new MissingMethodException(
                        $"Cannot find any raise method for event '{@event.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.EventRemove:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    @event = instruction.GrabEventReference(placeholder, out declaringType);
                    method = @event.RemoveMethod ?? throw new MissingMethodException(
                        $"Cannot find any remove method for event '{@event.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.IndexerGet:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    PropertyDefinition indexer = instruction.GrabIndexerReference(placeholder, out declaringType);
                    method = indexer.GetMethod ?? throw new MissingMethodException(
                        $"Cannot find any get method for indexer '{indexer.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.IndexerSet:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    indexer = instruction.GrabIndexerReference(placeholder, out declaringType);
                    method = indexer.SetMethod ?? throw new MissingMethodException(
                        $"Cannot find any set method for indexer '{indexer.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.Method:
                    method = instruction.GrabMethodReference(placeholder);
                    break;
                case ReferenceType.MethodMakeGeneric:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    TypeReference[] genericArugments = instruction.GrabTypeArray(placeholder, ArgumentName, out instruction);
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    method = instruction.GrabMethodReference(placeholder)
                        .MakeGenericMethod(genericArugments);
                    break;
                case ReferenceType.PropertyGet:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    PropertyDefinition property = instruction.GrabPropertyReference(placeholder, out declaringType);
                    method = property.GetMethod ?? throw new MissingMethodException(
                        $"Cannot find any get method for property '{property.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                case ReferenceType.PropertySet:
                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    property = instruction.GrabPropertyReference(placeholder, out declaringType);
                    method = property.SetMethod ?? throw new MissingMethodException(
                        $"Cannot find any set method for property '{property.FullName}'.")
                        .With(placeholder);
                    method = method.BindDeclaringType(declaringType);
                    break;
                default:
                    throw placeholder.Fail(ArgumentName);
            }

            return context.Module.ImportReference(method);
        }


        private static int GrabInt32(this Instruction instruction, Instruction placeholder, string argumentName)
        {
            var value = instruction.OpCode.Code switch
            {
                Code.Ldc_I4_M1 => -1,
                Code.Ldc_I4_0 => 0,
                Code.Ldc_I4_1 => 1,
                Code.Ldc_I4_2 => 2,
                Code.Ldc_I4_3 => 3,
                Code.Ldc_I4_4 => 4,
                Code.Ldc_I4_5 => 5,
                Code.Ldc_I4_6 => 6,
                Code.Ldc_I4_7 => 7,
                Code.Ldc_I4_8 => 8,
                Code.Ldc_I4_S => (sbyte)instruction.Operand,
                Code.Ldc_I4 => (int)instruction.Operand,
                _ => throw placeholder.Fail(argumentName),
            };

            instruction.Consume();
            return value;
        }

        private static string GrabString(this Instruction instruction, Instruction placeholder, string argumentName)
        {
            if (instruction.OpCode.Code is Code.Ldstr)
            {
                string value = (string)instruction.Operand;
                instruction.Consume();
                return value;
            }
            else
            {
                throw placeholder.Fail(argumentName);
            }
        }

        private static string[] GrabStringArray(this Instruction instruction, Instruction placeholder, string argumentName)
        {
            if (instruction.IsCallArrayEmpty())
            {
                instruction.Consume();
                return [];
            }

            var arrayStart = instruction.FindNewArrayStart(placeholder, "System.String", argumentName);
            arrayStart.Consume();

            int length = arrayStart
                .AcquirePrevious(placeholder, argumentName)
                .GrabInt32(placeholder, argumentName);

            var result = new string[length];

            for (var current = arrayStart.Next; current != placeholder; current = current.Next)
            {
                if (current.OpCode.Code is not Code.Dup)
                {
                    throw placeholder.Fail(argumentName);
                }

                current.Consume();
                current = current.Next;

                int index = current.GrabInt32(placeholder, argumentName);
                current = current.Next;

                if (current.OpCode.Code is not Code.Ldstr)
                {
                    throw placeholder.Fail(argumentName);
                }

                string value = (string)current.Operand;
                current.Consume();
                current = current.Next;

                if (current.OpCode.Code is not Code.Stelem_Ref)
                {
                    throw placeholder.Fail(argumentName);
                }

                current.Consume();
                result[index] = value;
            }

            for (int i = 0; i < length; i++)
            {
                if (result[i] is null)
                {
                    throw placeholder.Fail(argumentName);
                }
            }

            return result;
        }

        private static TypeReference GrabTypeReference(this Instruction instruction, Instruction placeholder, string argumentName)
        {
            bool isByRef;

            if (instruction.IsCallMakeByRefType())
            {
                isByRef = true;

                instruction.Consume();
                instruction = instruction.AcquirePrevious(placeholder, argumentName);
            }
            else
            {
                isByRef = false;
            }

            if (!instruction.IsCallRef(ReferenceType.Type))
            {
                throw placeholder.Fail(argumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, argumentName);

            if (!instruction.IsCallGetTypeFromHandle())
            {
                throw placeholder.Fail(argumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, argumentName);

            if (instruction.OpCode.Code is not Code.Ldtoken)
            {
                throw placeholder.Fail(argumentName);
            }

            var type = (TypeReference)instruction.Operand;
            instruction.Consume();

            return isByRef
                ? type.MakeByReferenceType()
                : type;
        }

        private static TypeReference[] GrabTypeArray(this Instruction instruction, Instruction placeholder, string argumentName, out Instruction start)
        {
            if (instruction.IsCallArrayEmpty())
            {
                start = instruction;

                instruction.Consume();
                return [];
            }

            var end = instruction.Next;
            var arrayStart = instruction.FindNewArrayStart(placeholder, "System.Type", argumentName);
            arrayStart.Consume();

            start = arrayStart.AcquirePrevious(placeholder, argumentName);
            int length = start.GrabInt32(placeholder, argumentName);

            var result = new TypeReference[length];

            for (var current = arrayStart.Next; current != end; current = current.Next)
            {
                if (current.OpCode.Code is not Code.Dup)
                {
                    placeholder.Fail(argumentName);
                }

                current.Consume();
                current = current.Next;

                int index = current.GrabInt32(placeholder, argumentName);
                current = current.Next;

                if (current.OpCode.Code is not Code.Ldtoken)
                {
                    throw placeholder.Fail(argumentName);
                }

                TypeReference type = (TypeReference)current.Operand;

                current.Consume();
                current = current.Next;

                if (!current.IsCallGetTypeFromHandle())
                {
                    throw placeholder.Fail(argumentName);
                }

                current.Consume();
                current = current.Next;

                if (current.IsCallMakeByRefType())
                {
                    type = type.MakeByReferenceType();

                    current.Consume();
                    current = current.Next;
                }

                if (current.OpCode.Code is not Code.Stelem_Ref)
                {
                    throw placeholder.Fail(argumentName);
                }

                current.Consume();
                result[index] = type;
            }

            for (int i = 0; i < length; i++)
            {
                if (result[i] is null)
                {
                    throw placeholder.Fail(argumentName);
                }
            }

            return result;
        }

        private static MethodReference GrabConstructorReference(this Instruction instruction, Instruction placeholder)
        {
            const string ArgumentName = "constructor reference";

            if (!instruction.IsCallRef(ReferenceType.Constructor))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            TypeReference[] parameterTypes = instruction.GrabTypeArray(placeholder, ArgumentName, out instruction);

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            TypeReference declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);

            MethodReference constructor = declaringType.GetConstructor(parameterTypes, placeholder) ?? throw new MissingMethodException(
                $"Cannot find any constructor with the specified parameter types in type '{declaringType.FullName}'.")
                .With(placeholder);

            return constructor.BindDeclaringType(declaringType);
        }

        private static EventDefinition GrabEventReference(this Instruction instruction, Instruction placeholder, out TypeReference declaringType)
        {
            const string ArgumentName = "event reference";

            if (!instruction.IsCallRef(ReferenceType.Event))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            string eventName = instruction.GrabString(placeholder, ArgumentName);

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
            return declaringType.GetEvent(eventName, placeholder) ?? throw new MissingMemberException(
                $"Cannot find any event named '{eventName}' in type '{declaringType.FullName}'.")
                .With(placeholder);
        }

        private static PropertyDefinition GrabIndexerReference(this Instruction instruction, Instruction placeholder, out TypeReference declaringType)
        {
            const string ArgumentName = "indexer reference";

            if (!instruction.IsCallRef(ReferenceType.Indexer))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            TypeReference[] parameterTypes = instruction.GrabTypeArray(placeholder, ArgumentName, out instruction);

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
            return declaringType.GetIndexer(parameterTypes, placeholder) ?? throw new MissingMemberException(
                $"Cannot find any indexer with the specified parameter types in type '{declaringType.FullName}'.")
                .With(placeholder);
        }

        private static MethodReference GrabMethodReference(this Instruction instruction, Instruction placeholder)
        {
            const string ArgumentName = "method reference";

            if (!instruction.IsCallRef(ReferenceType.Method, out int parameterCount))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            switch (parameterCount)
            {
                case 1:
                    string methodName = instruction.GrabString(placeholder, ArgumentName);

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    TypeReference declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
                    MethodReference method = declaringType.GetMethod(methodName, returnType: null, parameterTypes: null, placeholder)
                        ?? throw new MissingMethodException(
                            $"Cannot find any method named '{methodName}' in type '{declaringType.FullName}'.")
                            .With(placeholder);

                    return method.BindDeclaringType(declaringType);
                case 2:
                    TypeReference[] parameterTypes = instruction.GrabTypeArray(placeholder, ArgumentName, out instruction);

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    methodName = instruction.GrabString(placeholder, ArgumentName);

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
                    method = declaringType.GetMethod(methodName, returnType: null, parameterTypes, placeholder)
                        ?? throw new MissingMethodException(
                            $"Cannot find any method named '{methodName}' with the specified parameter types in type '{declaringType.FullName}'.")
                            .With(placeholder);

                    return method.BindDeclaringType(declaringType);
                case 3:
                    parameterTypes = instruction.GrabTypeArray(placeholder, ArgumentName, out instruction);

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    if (!instruction.IsCallGetTypeFromHandle())
                    {
                        throw placeholder.Fail(ArgumentName);
                    }

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    if (instruction.OpCode.Code is not Code.Ldtoken)
                    {
                        throw placeholder.Fail(ArgumentName);
                    }

                    TypeReference returenType = (TypeReference)instruction.Operand;

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    methodName = instruction.GrabString(placeholder, ArgumentName);

                    instruction.Consume();
                    instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

                    declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
                    method = declaringType.GetMethod(methodName, returenType, parameterTypes, placeholder)
                        ?? throw new MissingMethodException(
                            $"Cannot find any method named '{methodName}' with the specified return type and parameter types in type '{declaringType.FullName}'.")
                            .With(placeholder);

                    return method.BindDeclaringType(declaringType);
                default:
                    throw placeholder.Fail(ArgumentName);
            }
        }

        private static PropertyDefinition GrabPropertyReference(this Instruction instruction, Instruction placeholder, out TypeReference declaringType)
        {
            const string ArgumentName = "property reference";

            if (!instruction.IsCallRef(ReferenceType.Property))
            {
                throw placeholder.Fail(ArgumentName);
            }

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            string propertyName = instruction.GrabString(placeholder, ArgumentName);

            instruction.Consume();
            instruction = instruction.AcquirePrevious(placeholder, ArgumentName);

            declaringType = instruction.GrabTypeReference(placeholder, ArgumentName);
            return declaringType.GetProperty(propertyName, placeholder) ?? throw new MissingMemberException(
                $"Cannot find any property named '{propertyName}' in type '{declaringType.FullName}'.")
                .With(placeholder);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static InvalidProgramException Fail(this Instruction placeholder, string argumentName)
        {
            throw new InvalidProgramException(
                $"Cannot resolve target {argumentName}, the instruction sequence is incompatible.")
                .With(placeholder);
        }

        private static Instruction FindNewArrayStart(this Instruction instruction, Instruction placeholder, string elementType, string argumentName)
        {
            for (var current = instruction; current is not null; current = current.Previous)
            {
                if (current.OpCode.Code is Code.Newarr
                    && current.Operand is TypeReference type
                    && type.FullName.Equals(elementType, StringComparison.Ordinal))
                {
                    return current;
                }
            }

            throw placeholder.Fail(argumentName);
        }
    }
}
