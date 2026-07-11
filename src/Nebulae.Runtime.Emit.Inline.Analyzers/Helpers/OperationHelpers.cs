using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System;

namespace Nebulae.Runtime.Emit.Inline.Analyzers.Helpers
{
    internal static class OperationHelpers
    {
        public static IOperation GetInnermostConversion(this IOperation operation)
        {
            while (operation is IConversionOperation conversion)
            {
                operation = conversion.Operand;
            }

            return operation;
        }

        public static IOperation GetOutermostConversion(this IOperation operation)
        {
            while (operation.Parent is IConversionOperation conversion)
            {
                operation = conversion;
            }

            return operation;
        }

        public static bool IsArrayEmpty(this IOperation operation)
        {
            operation = operation.GetInnermostConversion();

            return operation is IInvocationOperation invocation
                && invocation.TargetMethod.Name == nameof(Array.Empty)
                && invocation.TargetMethod.ContainingType.SpecialType is SpecialType.System_Array;
        }

        public static bool IsTypeEmptyTypes(this IOperation operation, INamedTypeSymbol? systemType)
        {
            operation = operation.GetInnermostConversion();

            return operation is IFieldReferenceOperation fieldReference
                && fieldReference.Field.Name == nameof(Type.EmptyTypes)
                && SymbolEqualityComparer.Default.Equals(fieldReference.Field.ContainingType, systemType);
        }

        public static bool IsTypeOf(this IOperation operation)
        {
            return operation.GetInnermostConversion() is ITypeOfOperation;
        }

        public static bool IsInside(this IOperation operation, INamedTypeSymbol? symbol)
        {
            if (symbol is null)
            {
                return false;
            }

            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current.Type is INamedTypeSymbol namedType
                    && SymbolEqualityComparer.Default.Equals(namedType.OriginalDefinition, symbol))
                {
                    return true;
                }
            }

            return false;
        }

        public static IMethodSymbol? GetContainingMethod(
            this IOperation operation,
            Compilation compilation)
        {
            SemanticModel semanticModel = compilation.GetSemanticModel(operation.Syntax.SyntaxTree);
            return semanticModel.GetEnclosingSymbol(operation.Syntax.SpanStart) as IMethodSymbol;
        }

        public static bool TryGetValidLabel(this IOperation operation, out string label)
        {
            operation = operation.GetInnermostConversion();

            if (operation.ConstantValue.HasValue
                && operation.ConstantValue.Value is string value
                && value.Length is not 0)
            {
                label = value;
                return true;
            }

            label = string.Empty;
            return false;
        }

        public static bool VisitArrayItems(
            this IOperation operation,
            Action<IOperation> visitItem,
            Func<IOperation, bool>? isEmpty = null)
        {
            operation = operation.GetInnermostConversion();

            if (operation.IsArrayEmpty() || (isEmpty?.Invoke(operation) ?? false))
            {
                return true;
            }

            if (operation is IArrayCreationOperation { Initializer: { } initializer })
            {
                var values = initializer.ElementValues;

                for (int i = 0; i < values.Length; i++)
                {
                    visitItem(values[i]);
                }

                return true;
            }

            if (operation.Type is not IArrayTypeSymbol)
            {
                visitItem(operation);
                return true;
            }

            return false;
        }
    }
}
