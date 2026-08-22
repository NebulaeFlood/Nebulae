using Mono.Cecil;
using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class AssemblyReferenceHelpers
    {
        public const string PlaceholderAssemblyName = "Nebulae.Runtime.Emit.Inline";


        public static bool IsPlaceholderAssembly(this AssemblyNameReference reference)
        {
            return string.Equals(reference.Name, PlaceholderAssemblyName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsStrongNameSigned(this AssemblyDefinition assembly)
        {
            return (assembly.MainModule.Attributes & ModuleAttributes.StrongNameSigned) != 0;
        }

        public static bool ContainsDirectReference(this TypeReference? type)
        {
            if (type is null)
            {
                return false;
            }

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
    }
}
