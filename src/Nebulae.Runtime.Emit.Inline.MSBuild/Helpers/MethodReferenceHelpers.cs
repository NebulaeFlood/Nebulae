using Mono.Cecil;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class MethodReferenceHelpers
    {
        public static MethodReference BindDeclaringType(this MethodReference reference, TypeReference declaringType)
        {
            if (declaringType is not GenericInstanceType)
            {
                return reference;
            }

            var bound = new MethodReference(reference.Name, reference.ReturnType, declaringType)
            {
                CallingConvention = reference.CallingConvention,
                ExplicitThis = reference.ExplicitThis,
                HasThis = reference.HasThis
            };

            for (int i = 0; i < reference.Parameters.Count; i++)
            {
                bound.Parameters.Add(new ParameterDefinition(reference.Parameters[i].ParameterType));
            }

            for (int i = 0; i < reference.GenericParameters.Count; i++)
            {
                bound.GenericParameters.Add(new GenericParameter(reference.GenericParameters[i].Name, bound));
            }

            return bound;
        }

        public static CallSite MakeCallSite(this MethodReference reference)
        {
            var callSite = new CallSite(reference.ReturnType)
            {
                HasThis = reference.HasThis,
                ExplicitThis = reference.ExplicitThis,
                CallingConvention = reference.CallingConvention
            };

            var parameters = callSite.Parameters;
            var sourceParameters = reference.Parameters;

            for (int i = 0; i < sourceParameters.Count; i++)
            {
                parameters.Add(new ParameterDefinition(sourceParameters[i].ParameterType));
            }

            return callSite;
        }

        public static GenericInstanceMethod MakeGenericMethod(this MethodReference reference, TypeReference[] genericArguments)
        {
            var method = new GenericInstanceMethod(reference);
            var arguments = method.GenericArguments;

            for (int i = 0; i < genericArguments.Length; i++)
            {
                arguments.Add(genericArguments[i]);
            }

            return method;
        }
    }
}
