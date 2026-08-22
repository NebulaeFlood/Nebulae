using Mono.Cecil;
using Nebulae.Collections.Concurrent;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal sealed class AssemblyReferenceRequestGroup : ConcurrentHashSet<AssemblyReferenceIdentity>
    {
        public readonly AssemblyNameReference CanonicalReference;


        public string Name
        {
            get
            {
                string name = CanonicalReference.Name;

                foreach (var identity in this)
                {
                    if (string.CompareOrdinal(name, identity.Name) < 0)
                    {
                        name = identity.Name;
                    }
                }

                return name;
            }
        }


        public AssemblyReferenceRequestGroup(AssemblyNameReference reference)
        {
            Add(new AssemblyReferenceIdentity(reference));
            CanonicalReference = new AssemblyNameReference(reference.Name, reference.Version)
            {
                Attributes = reference.Attributes,
                Culture = reference.Culture,
                Hash = Clone(reference.Hash),
                HashAlgorithm = reference.HashAlgorithm,
                PublicKey = Clone(reference.PublicKey),
                PublicKeyToken = Clone(reference.PublicKeyToken),
            };

            static byte[] Clone(byte[]? value)
            {
                return value is null || value.Length is 0
                    ? []
                    : (byte[])value.Clone();
            }
        }
    }
}
