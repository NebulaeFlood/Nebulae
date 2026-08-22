using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Rewrite
{
    internal static class AssemblyRewriter
    {
        public static void Rewrite(InlineILTask task)
        {
            TaskLoggingHelper log = task.Log;

            log.LogMessage($"Loading assembly '{task.AssemblyPath}' for rewrite.");
            using var context = AssemblyRewriteContext.Create(task);

            log.LogMessage("Gathering rewrite targets.");
            var modules = context.Modules;
            var types = context.Types;
            var targets = types.AsParallel()
                .SelectMany(t => t.Methods)
                .Where(Placeholder.ReferencesPlaceholder)
                .ToArray();

            log.LogMessage($"Rewriting {targets.Length} method(s).");
            targets.AsParallel().ForAll(MethodRewriter.Rewrite);

            log.LogMessage("Resolving assembly references.");
            context.Importers.Complete();

            log.LogMessage($"Removing reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}'.");
            modules.UnloadReference();

            log.LogMessage("Writing rewritten assembly.");
            context.Assembly.Write(context.AssemblyPath, context.WriterParameters);

            log.LogMessage("Validating rewritten assembly.");
            context.ValidateRewrittenAssembly();

            if (targets.Length is 0)
            {
                log.LogMessage(
                    MessageImportance.High,
                    $"Successfully removed reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}' " +
                    $"for assembly '{context.Assembly.Name.Name}'.");
            }
            else
            {
                log.LogMessage(
                    MessageImportance.High,
                    $"Successfully rewritten {targets.Length} method(s) " +
                    $"in assembly '{context.Assembly.Name.Name}'.");
            }
        }

        private static void ValidateRewrittenAssembly(this scoped in AssemblyRewriteContext context)
        {
            using var assembly = AssemblyDefinition.ReadAssembly(
                context.AssemblyPath,
                new ReaderParameters(ReadingMode.Immediate)
                {
                    AssemblyResolver = context.Resolver,
                    InMemory = true,
                    ReadSymbols = context.ReadSymbols
                });

            var modules = assembly.Modules;

            for (int i = 0; i < modules.Count; i++)
            {
                var references = modules[i].AssemblyReferences;

                for (int j = 0; j < references.Count; j++)
                {
                    if (references[j].IsPlaceholderAssembly())
                    {
                        throw new InvalidProgramException(
                            $"Cannot remove the reference to '{AssemblyReferenceHelpers.PlaceholderAssemblyName}' " +
                            $"because module '{modules[i].Name}' in assembly '{assembly.Name.Name}' " +
                            $"remains member(s) that reference it.");
                    }
                }
            }

            if (!assembly.Name.PublicKeyToken.SequenceEqual(context.PublicKeyToken))
            {
                throw new InvalidOperationException(
                    $"Cannot rewrite assembly '{context.Assembly.Name.Name}' " +
                    $"because the rewrite operation changed its public key token.");
            }

            if (assembly.IsStrongNameSigned() != context.Assembly.IsStrongNameSigned())
            {
                throw new InvalidOperationException(
                    $"Cannot rewrite assembly '{context.Assembly.Name.Name}' " +
                    $"because the rewrite operation changed its strong-name signed state.");
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

                    if (target.Type.ContainsDirectReference())
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
