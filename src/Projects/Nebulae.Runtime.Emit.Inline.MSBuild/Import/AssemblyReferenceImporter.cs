using Mono.Cecil;
using Nebulae.Collections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal sealed class AssemblyReferenceImporter(ModuleDefinition module) : DefaultMetadataImporter(module)
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        public Collector<AssemblyReferenceConflictException> Conflicts
        {
            get
            {
                var conflicts = new Collector<AssemblyReferenceConflictException>();

                foreach (AssemblyReferenceRequestGroup group in _conflicts.Values)
                {
                    Collector<AssemblyNameReference> references = _existing[group.Name];
                    conflicts.Collect(Conflic(group, references));
                }

                foreach (AssemblyReferenceRequestGroup group in _pending.Values)
                {
                    if (group.Count > 1)
                    {
                        conflicts.Collect(Conflic(group, []));
                    }
                }

                return conflicts;

                AssemblyReferenceConflictException Conflic(AssemblyReferenceRequestGroup group, Collector<AssemblyNameReference> references)
                {
                    return new AssemblyReferenceConflictException(
                        group.Name,
                        module.Name,
#pragma warning disable IDE0305
#if NETSTANDARD2_0
                        references.Select(AssemblyReferenceIdentity.GetFullName).OrderBy(name => name, StringComparer.Ordinal).ToArray(),
                        group.Select(identity => identity.FullName).OrderBy(name => name, StringComparer.Ordinal).ToArray());
#else
                        references.Select(AssemblyReferenceIdentity.GetFullName).Order(StringComparer.Ordinal).ToArray(),
                        group.Select(identity => identity.FullName).Order(StringComparer.Ordinal).ToArray());
#endif
#pragma warning restore IDE0305
                }
            }
        }

        public string ModuleName
        {
            get => module.Name;
        }

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        public override AssemblyNameReference ImportReference(AssemblyNameReference reference)
        {
            if (_existing.TryGetValue(reference.Name, out var collector))
            {
                var references = collector.AsSpan();

                if (references.Length is 1)
                {
                    return references[0];
                }

                var identity = AssemblyReferenceIdentity.GetFullName(reference);

                for (int i = 0; i < references.Length; i++)
                {
                    AssemblyNameReference candidate = references[i];

                    if (identity.Equals(AssemblyReferenceIdentity.GetFullName(candidate), StringComparison.Ordinal))
                    {
                        return candidate;
                    }
                }

                _conflicts.GetOrAdd(reference.Name, Request)
                    .Add(new AssemblyReferenceIdentity(reference));
                return reference;
            }

            AssemblyReferenceRequestGroup group = _pending.GetOrAdd(reference.Name, Request);
            group.Add(new AssemblyReferenceIdentity(reference));
            return group.CanonicalReference;


            AssemblyReferenceRequestGroup Request(string name)
            {
                return new AssemblyReferenceRequestGroup(reference);
            }
        }

        public void Initialize()
        {
            foreach (var reference in module.AssemblyReferences)
            {
                if (!_existing.TryGetValue(reference.Name, out var collector))
                {
                    collector = new Collector<AssemblyNameReference>();
                    _existing.Add(reference.Name, collector);
                }

                collector.Collect(reference);
            }
        }

        public void Complete()
        {
            foreach (AssemblyReferenceRequestGroup group in _pending.Values
                .OrderBy(static group => group.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static group => group.Name, StringComparer.Ordinal))
            {
                module.AssemblyReferences.Add(group.CanonicalReference);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Fields
        //
        //------------------------------------------------------

        #region Private Fields

        private readonly ConcurrentDictionary<string, AssemblyReferenceRequestGroup> _conflicts =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, Collector<AssemblyNameReference>> _existing =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly ConcurrentDictionary<string, AssemblyReferenceRequestGroup> _pending =
            new(StringComparer.OrdinalIgnoreCase);

        #endregion
    }
}
