using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    internal static class AssemblyProcessor
    {
        public static int Process(AssemblyDefinition assembly, TaskLoggingHelper log)
        {
            log.LogMessage("Gathering targets.");
            var modules = assembly.GetModules();
            var types = modules.GetTypes();
            var methods = types.GetMethods();

            log.LogMessage($"Starting rewrite '{methods.Length}' methods.");
            methods.AsParallel().ForAll(Rewriter.Rewrite);

            log.LogMessage($"Removing reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}'.");
            assembly.ValidateReferenceIndependence(modules, types);
            modules.UnloadReference();

            return methods.Length;
        }


        private static ReadOnlySpan<ModuleDefinition> GetModules(this AssemblyDefinition assembly)
        {
            var modules = assembly.Modules;
            var result = new ModuleDefinition[modules.Count];

            int index = 0;

            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                var references = module.AssemblyReferences;

                for (int j = 0; j < references.Count; j++)
                {
                    if (references[j].IsPlaceholderAssembly())
                    {
                        result[index++] = module;
                        break;
                    }
                }
            }

            return new ReadOnlySpan<ModuleDefinition>(result, 0, index);
        }

        private static Collector<TypeDefinition> GetTypes(this ReadOnlySpan<ModuleDefinition> modules)
        {
            var collector = new Collector<TypeDefinition>(1024);

            for (int i = 0; i < modules.Length; i++)
            {
                CollectRange(collector, modules[i].Types);
            }

            return collector;


            static void CollectRange(Collector<TypeDefinition> collector, Collection<TypeDefinition> types)
            {
                collector.Collect(types);

                for (int i = 0; i < types.Count; i++)
                {
                    var type = types[i];

                    if (type.HasNestedTypes)
                    {
                        CollectRange(collector, type.NestedTypes);
                    }
                }
            }
        }

        private static MethodDefinition[] GetMethods(this Collector<TypeDefinition> types)
        {
            return types
                .AsParallel()
                .SelectMany(t => t.Methods)
                .Where(Placeholder.ReferencesPlaceholder)
                .ToArray();
        }

        private static void ValidateReferenceIndependence(this AssemblyDefinition assembly, ReadOnlySpan<ModuleDefinition> modules, Collector<TypeDefinition> types)
        {
            if (assembly.CustomAttributes.ContainsDirectReference())
            {
                throw new InvalidProgramException(
                    $"Cannot remove the reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}'. " +
                    $"An attribute of assembly '{assembly.Name.Name}' contains reference(s) " +
                    $"that cannot be safely removed.");
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i].CustomAttributes.ContainsDirectReference())
                {
                    throw new InvalidProgramException(
                        $"Cannot remove the reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}'. " +
                        $"An attribute of module '{modules[i].Name}' in assembly '{assembly.Name.Name}' contains reference(s) " +
                        $"that cannot be safely removed.");
                }
            }

            types.AsParallel().ForAll(ValidateReferenceIndependence);
        }

        private static void ValidateReferenceIndependence(this TypeDefinition type)
        {
            if (type.ContainsReference())
            {
                throw new InvalidProgramException(
                    $"Cannot remove the reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}'. " +
                    $"Type '{type.FullName}' in assembly '{type.Module.Assembly.Name.Name}' contains reference(s) " +
                    $"that cannot be safely removed.");
            }
        }

        private static void UnloadReference(this ReadOnlySpan<ModuleDefinition> modules)
        {
            for (int i = 0; i < modules.Length; i++)
            {
                var module = modules[i];

                if (module.HasSymbols)
                {
                    var imports = new HashSet<ImportDebugInformation>();
                    var methods = module.GetTypes().SelectMany(t => t.Methods);

                    foreach (var method in methods)
                    {
                        method.DebugInformation.Scope.UnloadReference(imports);
                    }
                }

                var references = module.AssemblyReferences;

                for (int j = references.Count - 1; j >= 0; j--)
                {
                    if (references[j].IsPlaceholderAssembly())
                    {
                        references.RemoveAt(j);
                    }
                }
            }
        }

        private static void UnloadReference(this ScopeDebugInformation? scope, HashSet<ImportDebugInformation> context)
        {
            if (scope is null)
            {
                return;
            }

            for (var import = scope.Import; import is not null && context.Add(import); import = import.Parent)
            {
                var targets = import.Targets;

                for (int i = targets.Count - 1; i >= 0; i--)
                {
                    ImportTarget target = targets[i];

                    if (target.AssemblyReference?.IsPlaceholderAssembly() ?? false)
                    {
                        targets.RemoveAt(i);
                        continue;
                    }

                    if (target.Type?.ContainsDirectReference() ?? false)
                    {
                        targets.RemoveAt(i);
                        continue;
                    }
                }
            }

            var scopes = scope.Scopes;

            for (int i = 0; i < scopes.Count; i++)
            {
                scopes[i].UnloadReference(context);
            }
        }
    }
}
