using Mono.Cecil;
using Mono.Collections.Generic;
using Nebulae.Collections;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using Nebulae.Runtime.Emit.Inline.MSBuild.Import;
using System;
using System.IO;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Rewrite
{
    internal readonly ref struct AssemblyRewriteContext : IDisposable
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        public readonly AssemblyDefinition Assembly;

        public readonly string AssemblyPath;

        public readonly AssemblyReferenceImporterProvider Importers;

        public readonly ReadOnlySpan<ModuleDefinition> Modules;

        public readonly bool ReadSymbols;

        public readonly IAssemblyResolver Resolver;

        public readonly byte[] PublicKeyToken;

        public readonly WriterParameters WriterParameters;

        #endregion


        public Collector<TypeDefinition> Types
        {
            get
            {
                var collector = new Collector<TypeDefinition>(1024);
                var modules = Modules;

                for (int i = 0; i < modules.Length; i++)
                {
                    CollectRange(collector, modules[i].Types);
                }

                return collector;


                static void CollectRange(Collector<TypeDefinition> collector, Collection<TypeDefinition> types)
                {
                    collector.CollectRange(types);

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
        }


        private AssemblyRewriteContext(
            AssemblyDefinition assembly,
            string assemblyPath,
            AssemblyReferenceImporterProvider importers,
            bool readSymbols,
            IAssemblyResolver resolver,
            WriterParameters writerParameters)
        {
            Assembly = assembly;
            AssemblyPath = assemblyPath;
            Importers = importers;
            Modules = GetModules(assembly);
            ReadSymbols = readSymbols;
            Resolver = resolver;
            PublicKeyToken = (byte[])assembly.Name.PublicKeyToken.Clone();
            WriterParameters = writerParameters;
        }


        public static AssemblyRewriteContext Create(InlineILTask task)
        {
            bool readSymbols = !string.IsNullOrEmpty(task.DebugType)
                && !task.DebugType.Equals("none", StringComparison.OrdinalIgnoreCase);

            var importers = new AssemblyReferenceImporterProvider();
            var resolver = new AssemblyResolver(task.References);

            AssemblyDefinition? assembly = null;

            try
            {
                assembly = AssemblyDefinition.ReadAssembly(task.AssemblyPath, new ReaderParameters(ReadingMode.Immediate)
                {
                    AssemblyResolver = resolver,
                    InMemory = true,
                    MetadataImporterProvider = importers,
                    ReadSymbols = readSymbols
                });

                WriterParameters writerParameters = ConfigureWriterParameters(
                    task,
                    assembly,
                    new WriterParameters { WriteSymbols = readSymbols });

                importers.Initialize(assembly);
                return new AssemblyRewriteContext(
                    assembly,
                    task.AssemblyPath,
                    importers,
                    readSymbols,
                    resolver,
                    writerParameters);
            }
            catch
            {
                assembly?.Dispose();
                resolver.Dispose();
                throw;
            }


        }

        public static bool RequiresRewrite(InlineILTask task, out string assemblyName)
        {
            using var assembly = AssemblyDefinition.ReadAssembly(task.AssemblyPath, new ReaderParameters(ReadingMode.Deferred)
            {
                InMemory = true
            });
            assemblyName = assembly.Name.Name;

            var modules = assembly.Modules;

            for (int i = 0; i < modules.Count; i++)
            {
                var references = modules[i].AssemblyReferences;

                for (int j = 0; j < references.Count; j++)
                {
                    if (references[j].IsPlaceholderAssembly())
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        public void Dispose()
        {
            Assembly.Dispose();
            Resolver.Dispose();
        }


        private static WriterParameters ConfigureWriterParameters(
            InlineILTask task,
            AssemblyDefinition assembly,
            WriterParameters parameters)
        {
            if (!assembly.Name.HasPublicKey
                || !assembly.IsStrongNameSigned()
                || task.PublicSign)
            {
                return parameters;
            }

            if (string.IsNullOrEmpty(task.KeyOriginatorFile))
            {
                throw new ArgumentException(
                    $"Cannot read assembly originator key file from empty path.");
            }

            try
            {
                parameters.StrongNameKeyBlob = File.ReadAllBytes(task.KeyOriginatorFile);
                return parameters;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(
                    $"Cannot read assembly originator key file " +
                    $"from path '{task.KeyOriginatorFile}'.", e);
            }
        }

        private static ReadOnlySpan<ModuleDefinition> GetModules(AssemblyDefinition assembly)
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
    }
}
