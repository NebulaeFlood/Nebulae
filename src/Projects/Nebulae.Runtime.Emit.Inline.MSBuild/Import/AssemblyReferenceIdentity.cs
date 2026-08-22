using Mono.Cecil;
using System;
using System.Text;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Import
{
    internal readonly struct AssemblyReferenceIdentity(AssemblyNameReference reference) : IEquatable<AssemblyReferenceIdentity>
    {
        public readonly string FullName = GetFullName(reference);
        public readonly string Name = reference.Name;


        public static string GetFullName(AssemblyNameReference reference)
        {
            var culture = reference.Culture;
            var display = new StringBuilder(128)
                .Append(reference.Name)
                .Append(", Version=")
                .Append(reference.Version.ToString(fieldCount: 4))
                .Append(", Culture=")
                .Append(string.IsNullOrEmpty(culture) ? "neutral" : culture)
                .Append(", PublicKeyToken=");

            byte[] token = reference.PublicKeyToken;

            if (token is { Length: > 0 })
            {
                for (int i = 0; i < token.Length; i++)
                {
                    display.Append(token[i].ToString("x2"));
                }
            }
            else
            {
                display.Append("null");
            }

            return display.ToString();
        }


        public override bool Equals(object? obj)
        {
            return obj is AssemblyReferenceIdentity other
                && FullName.Equals(other.FullName, StringComparison.Ordinal);
        }

        public bool Equals(AssemblyReferenceIdentity other)
        {
            return FullName.Equals(other.FullName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }

        public override string ToString()
        {
            return FullName;
        }
    }
}
