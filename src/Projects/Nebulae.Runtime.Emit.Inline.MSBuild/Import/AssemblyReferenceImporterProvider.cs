using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal sealed class AssemblyReferenceImporterProvider : IMetadataImporterProvider
    {
        public IMetadataImporter GetMetadataImporter(ModuleDefinition module)
        {
            return _importers.GetOrAdd(module, CreateImporter);
        }

        public void Complete()
        {
            var importers = _importers.Values
                .OrderBy(importer => importer.ModuleName, StringComparer.Ordinal)
                .ToArray();

            Validate(importers);

            foreach (var importer in importers)
            {
                importer.Complete();
            }
        }

        public void Initialize(AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            {
                _importers.GetOrAdd(module, CreateImporter)
                    .Initialize();
            }
        }


        private static AssemblyReferenceImporter CreateImporter(ModuleDefinition module)
        {
            return new AssemblyReferenceImporter(module);
        }

        private static void Validate(AssemblyReferenceImporter[] importers)
        {
            var conflicts = importers
                .SelectMany(importer => importer.Conflicts)
                .OrderBy(conflict => conflict.ModuleName, StringComparer.Ordinal)
                .OrderBy(conflict => conflict.AssemblyName, StringComparer.OrdinalIgnoreCase)
                .OrderBy(conflict => conflict.AssemblyName, StringComparer.Ordinal)
                .ToArray();

            if (conflicts.Length is 1)
            {
                throw conflicts[0];
            }

            if (conflicts.Length > 1)
            {
                throw new AggregateException(conflicts);
            }
        }


        private readonly ConcurrentDictionary<ModuleDefinition, AssemblyReferenceImporter> _importers = new();
    }
}
