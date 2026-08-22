using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Nebulae.Collections;
using System;
using System.Reflection;
using System.Text;

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

        public static bool Matches(this TypeReference left, TypeReference right, Instruction placeholder)
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
                return parameter.Matches(right, placeholder);
            }

            if (right is GenericParameter)
            {
                var definition = left.Resolve(placeholder);
                var attributes = definition?.CustomAttributes;

                for (int i = 0; i < attributes?.Count; i++)
                {
                    var attribute = attributes[i];

                    if (attribute.AttributeType.FullName.Equals(ReferenceAttributeFullName, StringComparison.Ordinal))
                    {
                        // GenericRef placeholder should match any generic parameter,
                        // and it should never appear in the right side.
                        return attribute.ConstructorArguments[0].Value is (int)ReferenceType.Generic;
                    }
                }

                return false;
            }

            if (left is ArrayType array)
            {
                return array.Matches(right, placeholder);
            }

            if (left is ByReferenceType byRef)
            {
                return byRef.Matches(right, placeholder);
            }

            if (left is PointerType pointer)
            {
                return pointer.Matches(right, placeholder);
            }

            if (left is GenericInstanceType generic)
            {
                return generic.Matches(right, placeholder);
            }

            if (left is FunctionPointerType function)
            {
                return function.Matches(right, placeholder);
            }

            if (left is IModifierType modifier)
            {
                return modifier.Matches(right, placeholder);
            }

            if (left is TypeSpecification || right is TypeSpecification)
            {
                return false;
            }

            if (!left.FullName.Equals(right.FullName, StringComparison.Ordinal))
            {
                return false;
            }

            ModuleDefinition lm = left.Resolve(placeholder).Module;
            ModuleDefinition rm = right.Resolve(placeholder).Module;


            // Exact module identity is sufficient even when no assembly identity is available.
            // Different builds normally have different MVIDs, so matching assembly families
            // is an alternative that preserves compatibility across assembly versions.
            return lm.Matches(rm)
                || lm.Assembly.Matches(rm.Assembly);
        }

        private static bool Matches(this GenericParameter left, TypeReference right, Instruction placeholder)
        {
            return right is GenericParameter parameter
                && left.Position == parameter.Position
                && left.Type == parameter.Type
                && Equals(left.Owner, parameter.Owner, placeholder);

            static bool Equals(
                IGenericParameterProvider left,
                IGenericParameterProvider right,
                Instruction placeholder)
            {
                if (left == right)
                {
                    return true;
                }

                if (left is MethodReference lm && right is MethodReference rm)
                {
                    return lm.FullName.Equals(rm.FullName, StringComparison.Ordinal)
                        && lm.DeclaringType.Matches(rm.DeclaringType, placeholder);
                }

                if (left is TypeReference lt && right is TypeReference rt)
                {
                    return lt.Matches(rt, placeholder);
                }

                return false;
            }
        }

        private static bool Matches(this ArrayType left, TypeReference right, Instruction placeholder)
        {
            if (right is not ArrayType array)
            {
                return false;
            }

            if (left.IsVector != array.IsVector
                || left.Rank != array.Rank)
            {
                return false;
            }

            var leftDimensions = left.Dimensions;
            var rightDimensions = array.Dimensions;

            if (leftDimensions.Count != rightDimensions.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Dimensions.Count; i++)
            {
                ArrayDimension leftDimension = left.Dimensions[i];
                ArrayDimension rightDimension = array.Dimensions[i];

                if (leftDimension.LowerBound != rightDimension.LowerBound
                    || leftDimension.UpperBound != rightDimension.UpperBound)
                {
                    return false;
                }
            }

            return left.ElementType.Matches(array.ElementType, placeholder);
        }

        private static bool Matches(this ByReferenceType left, TypeReference right, Instruction placeholder)
        {
            return right is ByReferenceType byRef
                && left.ElementType.Matches(byRef.ElementType, placeholder);
        }

        private static bool Matches(this PointerType left, TypeReference right, Instruction placeholder)
        {
            return right is PointerType pointer
                && left.ElementType.Matches(pointer.ElementType, placeholder);
        }

        private static bool Matches(this GenericInstanceType left, TypeReference right, Instruction placeholder)
        {
            if (right is not GenericInstanceType generic)
            {
                return false;
            }

            var leftArguments = left.GenericArguments;
            var rightArguments = generic.GenericArguments;

            if (leftArguments.Count != rightArguments.Count)
            {
                return false;
            }

            if (!left.ElementType.Matches(generic.ElementType, placeholder))
            {
                return false;
            }

            for (int i = 0; i < leftArguments.Count; i++)
            {
                if (!leftArguments[i].Matches(rightArguments[i], placeholder))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(this FunctionPointerType left, TypeReference right, Instruction placeholder)
        {
            if (right is not FunctionPointerType function)
            {
                return false;
            }

            if (left.HasThis != function.HasThis
                || left.ExplicitThis != function.ExplicitThis
                || left.CallingConvention != function.CallingConvention)
            {
                return false;
            }

            if (!left.ReturnType.Matches(function.ReturnType, placeholder))
            {
                return false;
            }

            var leftParameters = left.Parameters;
            var rightParameters = function.Parameters;

            if (rightParameters.Count != leftParameters.Count)
            {
                return false;
            }

            for (int i = 0; i < leftParameters.Count; i++)
            {
                if (!leftParameters[i].ParameterType.Matches(rightParameters[i].ParameterType, placeholder))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Matches(this IModifierType left, TypeReference right, Instruction placeholder)
        {
            return right is IModifierType modifier
                && left.GetType() == modifier.GetType()
                && left.ModifierType.Matches(modifier.ModifierType, placeholder)
                && left.ElementType.Matches(modifier.ElementType, placeholder);
        }

        private static bool Matches(this ModuleDefinition left, ModuleDefinition right)
        {
            return left.Mvid != Guid.Empty
                && left.Mvid == right.Mvid
                && left.MetadataToken.Equals(right.MetadataToken);
        }

        private static bool Matches(this AssemblyDefinition? left, AssemblyDefinition? right)
        {
            return left is not null
                && right is not null
                && Equals(left.Name, right.Name);


            static bool Equals(AssemblyNameReference left, AssemblyNameReference right)
            {
                return left.Name.Equals(right.Name, StringComparison.OrdinalIgnoreCase)
                    && Normalize(left.Culture).Equals(Normalize(right.Culture), StringComparison.OrdinalIgnoreCase)
                    && left.PublicKeyToken.SequenceEqual(right.PublicKeyToken);
            }

            static string Normalize(string? culture)
            {
                return string.IsNullOrEmpty(culture)
                    ? "neutral"
                    : culture!;
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Member Searchers
        //
        //------------------------------------------------------

        #region Member Searchers

        public static MethodDefinition? GetConstructor(
            this TypeReference reference,
            TypeReference[] parameterTypes,
            Instruction placeholder)
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

                if (method.IsConstructor && parameterTypes.SequenceEqual(method.Parameters, placeholder))
                {
                    return method;
                }
            }

            return null;
        }

        public static EventDefinition? GetEvent(
            this TypeReference reference,
            string eventName,
            Instruction placeholder)
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

        public static FieldDefinition? GetField(
            this TypeReference reference,
            string fieldName,
            Instruction placeholder)
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

        public static PropertyDefinition? GetIndexer(
            this TypeReference reference,
            TypeReference[] parameterTypes,
            Instruction placeholder)
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

                if (parameterTypes.SequenceEqual(property.Parameters, placeholder))
                {
                    return property;
                }
            }

            return null;
        }

        public static MethodDefinition? GetMethod(
            this TypeReference reference,
            string methodName,
            TypeReference[] parameterTypes,
            Instruction placeholder)
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
                    && method.GenericParameters.Count is 0
                    && parameterTypes.SequenceEqual(method.Parameters, placeholder))
                {
                    return method;
                }
            }

            return null;
        }

        public static MethodDefinition? GetMethod(
            this TypeReference reference,
            string methodName,
            int genericParameterCount,
            TypeReference[] parameterTypes,
            Instruction placeholder)
        {
            if (reference is GenericParameter)
            {
                return null;
            }

            var definition = reference.Resolve(placeholder);
            var methods = definition.Methods;
            var matches = new ValueCollector<MethodDefinition>(4);

            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];

                if (method.Name.Equals(methodName, StringComparison.Ordinal)
                    && method.GenericParameters.Count == genericParameterCount
                    && parameterTypes.SequenceEqual(method.Parameters, placeholder))
                {
                    matches.Collect(method);
                }
            }

            if (matches.IsEmpty)
            {
                return null;
            }

            if (matches.Count is 1)
            {
                return matches[0];
            }

            var candidates = new StringBuilder(128);
            var span = matches.AsSpan();

            for (int i = 0; i < span.Length; i++)
            {
                candidates.AppendLine()
                    .Append('\t')
                    .Append(span[i].FullName);
            }

            throw new AmbiguousMatchException(
                $"Ambiguous match for method '{methodName}' " +
                $"with generic parameter count '{genericParameterCount}' " +
                $"in type '{reference.FullName}'. Candidates:{candidates}")
                .With(placeholder);
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
            try
            {
                return reference.Resolve()
                    ?? throw new InvalidOperationException(
                        $"Cannot resolve type reference '{GetFullName(reference)}'.")
                        .With(placeholder);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot resolve type reference '{GetFullName(reference)}'.",
                    e).With(placeholder);
            }

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

        private static bool SequenceEqual(
            this TypeReference[] left,
            Collection<ParameterDefinition> right,
            Instruction placeholder)
        {
            if (left.Length != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!left[i].Matches(right[i].ParameterType, placeholder))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
