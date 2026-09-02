using System;
using System.Text;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal sealed class AssemblyReferenceConflictException(
        string assemblyName,
        string moduleName,
        string[] existingIdentities,
        string[] requestedIdentities)
            : InvalidOperationException(CreateMessage(assemblyName, moduleName, existingIdentities, requestedIdentities))
    {
        public readonly string AssemblyName = assemblyName;
        public readonly string ModuleName = moduleName;

        public readonly string[] ExistingIdentities = existingIdentities;
        public readonly string[] RequestedIdentities = requestedIdentities;


        private static string CreateMessage(
            string assemblyName,
            string moduleName,
            string[] existingIdentities,
            string[] requestedIdentities)
        {
            var builder = new StringBuilder(256)
                .Append($"Cannot import assembly reference '")
                .Append(assemblyName)
                .Append($"' into module '")
                .Append(moduleName)
                .Append("'.");

            if (existingIdentities.Length > 0)
            {
                builder.AppendLine()
                    .AppendLine("The module already contains a reference with this name:")
                    .Append("- ")
                    .Append(existingIdentities[0]);
            }

            builder.AppendLine()
                .AppendLine("Imported members require incompatible identities:");

            for (int i = 0; i < requestedIdentities.Length; i++)
            {
                builder.Append("- ")
                    .AppendLine(requestedIdentities[i]);
            }

            return builder.Append(
                "Reference the intended assembly from " +
                "the target module or align the imported dependencies.")
                .ToString();
        }
    }
}
