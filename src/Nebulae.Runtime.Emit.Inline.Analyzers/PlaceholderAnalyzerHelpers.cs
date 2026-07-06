using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal static class PlaceholderAnalyzerHelpers
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

        public static bool ContainsAttribute(this ISymbol symbol, params ReadOnlySpan<INamedTypeSymbol> attributeTypes)
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

        public static IOperation GetOutermostConversion(this IOperation operation)
        {
            while (operation.Parent is IConversionOperation conversion)
            {
                operation = conversion;
            }

            return operation;
        }

        public static ReferenceType GetReferenceType(this ISymbol symbol, INamedTypeSymbol referenceAttributeType)
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

        public static bool IsInside(this IOperation operation, INamedTypeSymbol? symbol)
        {
            if (symbol is null)
            {
                return false;
            }

            for (var current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current.Type is INamedTypeSymbol namedType
                    && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, symbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}