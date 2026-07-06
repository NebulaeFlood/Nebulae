using Mono.Cecil;
using Mono.Collections.Generic;
using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class AssemblyReferenceHelpers
    {
        public const string PlaceholderAssemblyName = "Nebulae.Runtime.Emit.Inline";


        public static bool IsPlaceholderAssembly(this AssemblyNameReference reference)
        {
            return string.Equals(reference.Name, PlaceholderAssemblyName, StringComparison.Ordinal);
        }

        public static bool ContainsReference(this TypeDefinition type)
        {
            if (type.CustomAttributes.ContainsDirectReference())
            {
                return true;
            }

            if (type.GenericParameters.ContainsDirectReference())
            {
                return true;
            }

            if (type.BaseType.GetGenericArguments()?.ContainsDirectReference() ?? false)
            {
                return true;
            }

            if (type.Interfaces.ContainsDirectReference())
            {
                return true;
            }

            if (type.Events.ContainsDirectReference())
            {
                return true;
            }

            if (type.Fields.ContainsDirectReference())
            {
                return true;
            }

            if (type.Methods.ContainsDirectReference())
            {
                return true;
            }

            if (type.Properties.ContainsDirectReference())
            {
                return true;
            }

            return false;
        }

        public static bool ContainsDirectReference(this TypeReference type)
        {
            if (type.Scope is AssemblyNameReference assembly && assembly.IsPlaceholderAssembly())
            {
                return true;
            }

            if (type is GenericInstanceType genericInstance)
            {
                if (genericInstance.ElementType.ContainsDirectReference())
                {
                    return true;
                }

                for (int i = 0; i < genericInstance.GenericArguments.Count; i++)
                {
                    if (genericInstance.GenericArguments[i].ContainsDirectReference())
                    {
                        return true;
                    }
                }

                return false;
            }

            if (type is IModifierType modifier)
            {
                return modifier.ElementType.ContainsDirectReference()
                    || modifier.ModifierType.ContainsDirectReference();
            }

            if (type is FunctionPointerType functionPointer)
            {
                if (functionPointer.ReturnType.ContainsDirectReference())
                {
                    return true;
                }

                for (int i = 0; i < functionPointer.Parameters.Count; i++)
                {
                    if (functionPointer.Parameters[i].ParameterType.ContainsDirectReference())
                    {
                        return true;
                    }
                }

                return false;
            }

            if (type is TypeSpecification specification)
            {
                return specification.ElementType.ContainsDirectReference();
            }

            return false;
        }

        public static bool ContainsDirectReference(this MethodDefinition method)
        {
            if (method.ReturnType.ContainsDirectReference())
            {
                return true;
            }

            if (method.GenericParameters.ContainsDirectReference())
            {
                return true;
            }

            if (method.Parameters.ContainsDirectReference())
            {
                return true;
            }

            if (method.CustomAttributes.ContainsDirectReference())
            {
                return true;
            }

            if (method.MethodReturnType.CustomAttributes.ContainsDirectReference())
            {
                return true;
            }

            if (!method.HasBody)
            {
                return false;
            }

            var body = method.Body;
            var variables = body.Variables;

            for (int i = 0; i < variables.Count; i++)
            {
                if (variables[i].VariableType.ContainsDirectReference())
                {
                    return true;
                }
            }

            var instructions = body.Instructions;

            for (int i = 0; i < instructions.Count; i++)
            {
                switch (instructions[i].Operand)
                {
                    case CallSite site:
                        if (site.ContainsDirectReference())
                        {
                            return true;
                        }
                        break;
                    case FieldReference field:
                        if (field.FieldType.ContainsDirectReference())
                        {
                            return true;
                        }

                        if (field.DeclaringType.ContainsDirectReference())
                        {
                            return true;
                        }
                        break;
                    case MethodReference methodRef:
                        if (methodRef.ContainsDirectReference())
                        {
                            return true;
                        }
                        break;
                    case TypeReference type:
                        if (type.ContainsDirectReference())
                        {
                            return true;
                        }
                        break;
                    default:
                        break;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this MethodReference method)
        {
            if (method.DeclaringType.ContainsDirectReference())
            {
                return true;
            }

            if (method.ReturnType.ContainsDirectReference())
            {
                return true;
            }

            if (method.GetGenericArguments()?.ContainsDirectReference() ?? false)
            {
                return true;
            }

            if (method.Parameters.ContainsDirectReference())
            {
                return true;
            }

            if (method.MethodReturnType.CustomAttributes.ContainsDirectReference())
            {
                return true;
            }

            return false;
        }

        public static bool ContainsDirectReference(this CallSite site)
        {
            if (site.ReturnType.ContainsDirectReference())
            {
                return true;
            }

            if (site.Parameters.ContainsDirectReference())
            {
                return true;
            }

            if (site.MethodReturnType.CustomAttributes.ContainsDirectReference())
            {
                return true;
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<TypeReference> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                if (types[i].ContainsDirectReference())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<CustomAttribute> attributes)
        {
            for (int i = 0; i < attributes.Count; i++)
            {
                var attribute = attributes[i];

                if (attribute.AttributeType.ContainsDirectReference())
                {
                    return true;
                }

                for (int j = 0; j < attribute.ConstructorArguments.Count; j++)
                {
                    if (ContainsDirectReference(attribute.ConstructorArguments[j]))
                    {
                        return true;
                    }
                }

                for (int j = 0; j < attribute.Fields.Count; j++)
                {
                    if (ContainsDirectReference(attribute.Fields[j].Argument))
                    {
                        return true;
                    }
                }

                for (int j = 0; j < attribute.Properties.Count; j++)
                {
                    if (ContainsDirectReference(attribute.Properties[j].Argument))
                    {
                        return true;
                    }
                }
            }

            return false;

            static bool ContainsDirectReference(CustomAttributeArgument argument)
            {
                if (argument.Type.ContainsDirectReference())
                {
                    return true;
                }

                if (argument.Value is TypeReference type)
                {
                    return type.ContainsDirectReference();
                }

                if (argument.Value is CustomAttributeArgument[] arguments)
                {
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        if (ContainsDirectReference(arguments[i]))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public static bool ContainsDirectReference(this Collection<GenericParameter> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                if (parameter.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }

                if (ContainsDirectReference(parameter.Constraints))
                {
                    return true;
                }
            }

            return false;

            static bool ContainsDirectReference(Collection<GenericParameterConstraint> constraints)
            {
                for (int i = 0; i < constraints.Count; i++)
                {
                    var constraint = constraints[i];

                    if (constraint.ConstraintType.ContainsDirectReference())
                    {
                        return true;
                    }

                    if (constraint.CustomAttributes.ContainsDirectReference())
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public static bool ContainsDirectReference(this Collection<InterfaceImplementation> interfaces)
        {
            for (int i = 0; i < interfaces.Count; i++)
            {
                var @interface = interfaces[i];

                if (@interface.InterfaceType.GetGenericArguments()?.ContainsDirectReference() ?? false)
                {
                    return true;
                }

                if (@interface.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<ParameterDefinition> parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];

                if (parameter.ParameterType.ContainsDirectReference())
                {
                    return true;
                }

                if (parameter.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<EventDefinition> events)
        {
            for (int i = 0; i < events.Count; i++)
            {
                var @event = events[i];

                if (@event.EventType.ContainsDirectReference())
                {
                    return true;
                }

                if (@event.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }

                if (@event.AddMethod?.ContainsDirectReference() ?? false)
                {
                    return true;
                }

                if (@event.RemoveMethod?.ContainsDirectReference() ?? false)
                {
                    return true;
                }

                if (@event.InvokeMethod?.ContainsDirectReference() ?? false)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<FieldDefinition> fields)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];

                if (field.FieldType.ContainsDirectReference())
                {
                    return true;
                }

                if (field.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<MethodDefinition> methods)
        {
            for (int i = 0; i < methods.Count; i++)
            {
                var method = methods[i];

                if (method.ContainsDirectReference())
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsDirectReference(this Collection<PropertyDefinition> properties)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                if (property.PropertyType.ContainsDirectReference())
                {
                    return true;
                }

                if (property.Parameters.ContainsDirectReference())
                {
                    return true;
                }

                if (property.CustomAttributes.ContainsDirectReference())
                {
                    return true;
                }

                if (property.GetMethod?.ContainsDirectReference() ?? false)
                {
                    return true;
                }

                if (property.SetMethod?.ContainsDirectReference() ?? false)
                {
                    return true;
                }
            }

            return false;
        }


        private static Collection<TypeReference>? GetGenericArguments(this TypeReference? type)
        {
            return type is GenericInstanceType instance
                ? instance.GenericArguments
                : null;
        }

        private static Collection<TypeReference>? GetGenericArguments(this MethodReference? method)
        {
            return method is GenericInstanceMethod instance
                ? instance.GenericArguments
                : null;
        }
    }
}
