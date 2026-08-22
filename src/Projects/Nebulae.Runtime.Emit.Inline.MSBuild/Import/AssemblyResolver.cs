using Mono.Cecil;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal sealed class AssemblyResolver : IAssemblyResolver
    {
        public AssemblyResolver(string[] references)
        {
            try
            {
                for (int i = 0; i < references.Length; i++)
                {
                    string path = references[i];
                    var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters(ReadingMode.Deferred)
                    {
                        AssemblyResolver = this
                    });

                    _assemblies.Add(AssemblyReferenceIdentity.GetFullName(assembly.Name), assembly);
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }


        public AssemblyDefinition Resolve(AssemblyNameReference name)
        {
            var identity = AssemblyReferenceIdentity.GetFullName(name);

            if (_assemblies.TryGetValue(identity, out var assembly))
            {
                return assembly;
            }

            throw new AssemblyResolutionException(name);
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            return Resolve(name);
        }

        public void Dispose()
        {
            foreach (var assembly in _assemblies.Values)
            {
                assembly.Dispose();
            }

            _assemblies.Clear();
        }


        private readonly Dictionary<string, AssemblyDefinition> _assemblies =
            new(StringComparer.Ordinal);
    }
}
