using Microsoft.CodeAnalysis;
using System;

namespace Nebulae.Runtime.Emit.Inline.Analyzers.Helpers
{
    internal static class SymbolHelpers
    {
        public static bool ContainsAttribute(this ISymbol symbol, INamedTypeSymbol attributeType)
        {
            var attributes = symbol.OriginalDefinition.GetAttributes();

            for (int i = 0; i < attributes.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(attributes[i].AttributeClass, attributeType))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool ContainsAttribute(
            this ISymbol symbol,
            params ReadOnlySpan<INamedTypeSymbol> attributeTypes)
        {
            var attributes = symbol.OriginalDefinition.GetAttributes();

            for (int i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];

                for (int j = 0; j < attributeTypes.Length; j++)
                {
                    if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, attributeTypes[j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static PlaceholderInfo GetPlaceholderInfo(
            this ISymbol symbol,
            INamedTypeSymbol placeholderAttributeType)
        {
            var attributes = symbol.OriginalDefinition.GetAttributes();

            for (int i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, placeholderAttributeType))
                {
                    return new PlaceholderInfo(
                        (PlaceholderCode)attribute.ConstructorArguments[0].Value!,
                        (PlaceholderOperand)attribute.ConstructorArguments[1].Value!);
                }
            }

            throw new InvalidOperationException("Symbol does not have the specified placeholder attribute.");
        }

        public static ReferenceType GetReferenceType(
            this ISymbol symbol,
            INamedTypeSymbol referenceAttributeType)
        {
            var attributes = symbol.OriginalDefinition.GetAttributes();

            for (int i = 0; i < attributes.Length; i++)
            {
                var attribute = attributes[i];

                if (SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, referenceAttributeType))
                {
                    return (ReferenceType)attribute.ConstructorArguments[0].Value!;
                }
            }

            throw new InvalidOperationException("Symbol does not have the specified reference attribute.");
        }

        public static bool IsInside(this IMethodSymbol method, IMethodSymbol owningMethod)
        {
            for (IMethodSymbol? current = method;
                current is not null;
                current = current.ContainingSymbol as IMethodSymbol)
            {
                if (SymbolEqualityComparer.Default.Equals(current, owningMethod))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
