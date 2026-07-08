using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class TypeReferenceHelpers
    {
        internal const string ReferenceAttributeFullName = "Nebulae.Runtime.Emit.Inline.ReferenceAttribute";


        //------------------------------------------------------
        //
        //  Type Matchers
        //
        //------------------------------------------------------

        #region Type Matchers

        public static bool Matches(this TypeReference left, TypeReference right)
        {
            if (left == right)
            {
                return true;
            }

            if (left is null || right is null)
            {
                return false;
            }

            if (left is GenericParameter parameter)
            {
                return parameter.Matches(right);
            }

            if (right is GenericParameter)
            {
                // Generic placeholder should match any generic parameter,
                // but it should not appear in the right side.
                var definition = left.Resolve();
                var attributes = definition?.CustomAttributes;

                for (int i = 0; i < attributes?.Count; i++)
                {
                    var attribute = attributes[i];

                    if (attribute.AttributeType.FullName.Equals(ReferenceAttributeFullName, StringComparison.Ordinal))
                    {
                        return attribute.ConstructorArguments[0].Value is (int)ReferenceType.Generic;
                    }
                }

                return false;
            }

            if (left is ArrayType array)
            {
                return array.Matches(right);
            }

            if (left is ByReferenceType byRef)
            {
                return byRef.Matches(right);
            }

            if (left is PointerType pointer)
            {
                return pointer.Matches(right);
            }

            if (left is GenericInstanceType generic)
            {
                return generic.Matches(right);
            }

            if (left is FunctionPointerType function)
            {
                return function.Matches(right);
            }

            if (left is IModifierType modifier)
            {
                return modifier.Matches(right);
            }

            if (right is TypeSpecification)
            {
                return false;
            }

            if (!left.FullName.Equals(right.FullName, StringComparison.Ordinal))
            {
                return false;
            }

            return Equals(left.DeclaringType, right.DeclaringType);

            static bool Equals(TypeReference? left, TypeReference? right)
            {
                if (left == right)
                {
                    return true;
                }

                if (left is null || right is null)
                {
                    return false;
                }

                if (!left.FullName.Equals(right.FullName, StringComparison.Ordinal))
                {
                    return false;
                }

                return Equals(left.DeclaringType, right.DeclaringType);
            }
        }

        private static bool Matches(this GenericParameter left, TypeReference right)
        {
            return right is GenericParameter parameter
                && left.Position == parameter.Position
                && left.Type == parameter.Type
                && Equals(left.Owner, parameter.Owner);

            static bool Equals(IGenericParameterProvider left, IGenericParameterProvider right)
            {
                if (left == right)
                {
                    return true;
                }

                if (left is MethodReference lm && right is MethodReference rm)
                {
                    return lm.FullName.Equals(rm.FullName, StringComparison.Ordinal);
                }

                if (left is TypeReference lt && right is TypeReference rt)
                {
                    return lt.FullName.Equals(rt.FullName, StringComparison.Ordinal);
                }

                return false;
            }
        }

        private static bool Matches(this ArrayType left, TypeReference right)
        {
            return right is ArrayType array
                && left.Rank == array.Rank
                && left.ElementType.Matches(array.ElementType);
        }

        private static bool Matches(this ByReferenceType left, TypeReference right)
        {
            return right is ByReferenceType byRef
                && left.ElementType.Matches(byRef.ElementType);
        }

        private static bool Matches(this PointerType left, TypeReference right)
        {
            return right is PointerType pointer
                && left.ElementType.Matches(pointer.ElementType);
        }

        private static bool Matches(this GenericInstanceType left, TypeReference right)
        {
            if (right is not GenericInstanceType generic)
            {
                return false;
            }

            if (!left.ElementType.Matches(generic.ElementType))
            {
                return false;
            }

            var leftArguments = left.GenericArguments;
            var rightArguments = generic.GenericArguments;

            if (leftArguments.Count != rightArguments.Count)
            {
                return false;
            }

            for (int i = 0; i < leftArguments.Count; i++)
            {
                if (!leftArguments[i].Matches(rightArguments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(this FunctionPointerType left, TypeReference right)
        {
            if (right is not FunctionPointerType function)
            {
                return false;
            }

            if (!left.ReturnType.Matches(function.ReturnType))
            {
                return false;
            }

            var leftParameters = left.Parameters;
            var rightParameters = function.Parameters;

            if (leftParameters.Count != rightParameters.Count)
            {
                return false;
            }

            for (int i = 0; i < leftParameters.Count; i++)
            {
                if (!leftParameters[i].ParameterType.Matches(rightParameters[i].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(this IModifierType left, TypeReference right)
        {
            return right is IModifierType modifier
                && left.ModifierType.Matches(modifier.ModifierType)
                && left.ElementType.Matches(modifier.ElementType);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Member Searchers
        //
        //------------------------------------------------------

        #region Member Searchers

        public static MethodDefinition? GetConstructor(this TypeReference reference, TypeReference[] parameterTypes, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var methods = definition.Methods;

            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];

                if (method.IsConstructor && parameterTypes.SequenceEqual(method.Parameters))
                {
                    return method;
                }
            }

            return null;
        }

        public static EventDefinition? GetEvent(this TypeReference reference, string eventName, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var events = definition.Events;

            for (int i = 0; i < events.Count; i++)
            {
                var @event = events[i];

                if (@event.Name.Equals(eventName, StringComparison.Ordinal))
                {
                    return @event;
                }
            }

            return null;
        }

        public static FieldDefinition? GetField(this TypeReference reference, string fieldName, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var fields = definition.Fields;

            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];

                if (field.Name.Equals(fieldName, StringComparison.Ordinal))
                {
                    return field;
                }
            }

            return null;
        }

        public static PropertyDefinition? GetIndexer(this TypeReference reference, TypeReference[] parameterTypes, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var properties = definition.Properties;

            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                if (parameterTypes.SequenceEqual(property.Parameters))
                {
                    return property;
                }
            }

            return null;
        }

        public static MethodDefinition? GetMethod(this TypeReference reference, string methodName, TypeReference? returnType, TypeReference[]? parameterTypes, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var methods = definition.Methods;

            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];

                if (method.Name.Equals(methodName, StringComparison.Ordinal)
                    && (returnType is null || returnType.Matches(method.ReturnType))
                    && (parameterTypes is null || parameterTypes.SequenceEqual(method.Parameters)))
                {
                    return method;
                }
            }

            return null;
        }

        public static PropertyDefinition? GetProperty(this TypeReference reference, string propertyName, Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var properties = definition.Properties;

            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                if (property.Name.Equals(propertyName, StringComparison.Ordinal))
                {
                    return property;
                }
            }

            return null;
        }

        #endregion


        private static TypeDefinition Resolve(this TypeReference reference, Instruction placeholder)
        {
            return reference.Resolve()
                ?? throw new InvalidOperationException(
                    $"Cannot resolve type reference '{GetFullName(reference)}'.")
                    .With(placeholder);

            static string GetFullName(TypeReference reference)
            {
                var scope = reference.Scope;

                if (scope is AssemblyNameReference assemblyName)
                {
                    return $"{reference.FullName}, {assemblyName.FullName}";
                }

                if (scope is ModuleDefinition moduleDef && moduleDef.Assembly is AssemblyDefinition assembly)
                {
                    return $"{reference.FullName}, {assembly.FullName}";
                }

                if (scope is ModuleReference moduleRef)
                {
                    return $"{reference.FullName}, {moduleRef.Name}";
                }

                return reference.FullName;
            }
        }

        private static bool SequenceEqual(this TypeReference[] left, Collection<ParameterDefinition> right)
        {
            if (left.Length != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Matches(right[i].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
