using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Generic;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal sealed class PlaceholderAnalyzerState(INamedTypeSymbol placeholderAttribute, INamedTypeSymbol referenceAttribute, IAssemblySymbol placeholderAssembly, INamedTypeSymbol? expressionType)
    {
        public readonly INamedTypeSymbol PlaceholderAttribute = placeholderAttribute;
        public readonly INamedTypeSymbol ReferenceAttribute = referenceAttribute;
        public readonly IAssemblySymbol PlaceholderAssembly = placeholderAssembly;
        public readonly INamedTypeSymbol? ExpressionType = expressionType;


        public void AnalyzeDeclaredSymbol(SymbolAnalysisContext context)
        {
            ISymbol symbol = context.Symbol;

            if (symbol.IsImplicitlyDeclared)
            {
                return;
            }

            var placeholders = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

            switch (symbol)
            {
                case IEventSymbol @event:
                    SearchDirectPlaceholders(@event.Type, placeholders);
                    break;
                case IFieldSymbol field:
                    SearchDirectPlaceholders(field.Type, placeholders);
                    break;
                case IMethodSymbol method:
                    SearchDirectPlaceholders(method.ReturnType, placeholders);

                    var parameters = method.Parameters;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        SearchDirectPlaceholders(parameters[i].Type, placeholders);
                    }

                    var parameterTypes = method.TypeParameters;

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        var constraints = parameterTypes[i].ConstraintTypes;

                        for (int j = 0; j < constraints.Length; j++)
                        {
                            SearchDirectPlaceholders(constraints[j], placeholders);
                        }
                    }

                    break;
                case IPropertySymbol property:
                    SearchDirectPlaceholders(property.Type, placeholders);

                    parameters = property.Parameters;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        SearchDirectPlaceholders(parameters[i].Type, placeholders);
                    }

                    break;
                case INamedTypeSymbol type:
                    SearchDirectPlaceholders(type, placeholders);

                    var interfaces = type.Interfaces;

                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        SearchDirectPlaceholders(interfaces[i], placeholders);
                    }

                    parameterTypes = type.TypeParameters;

                    for (int i = 0; i < parameterTypes.Length; i++)
                    {
                        SearchDirectPlaceholders(parameterTypes[i], placeholders);
                    }

                    if (type.BaseType is not null)
                    {
                        SearchDirectPlaceholders(type.BaseType, placeholders);
                    }

                    break;
                default:
                    break;
            }

            foreach (var placeholder in placeholders)
            {
                var location = symbol.Locations.FirstOrDefault(candidate => candidate.IsInSource);

                if (location is not null)
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            PlaceholderAnalyzer.PlaceholderTypeRule,
                            location,
                            symbol.Name,
                            placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                }
            }
        }

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;

            if (method.ContainsAttribute(PlaceholderAttribute))
            {
                if (context.ContainingSymbol is not IMethodSymbol || invocation.IsInside(ExpressionType))
                {
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            PlaceholderAnalyzer.InvalidContextRule,
                            invocation.Syntax.GetLocation(),
                            method.Name));
                }

                return;
            }

            if (!method.ContainsAttribute(ReferenceAttribute))
            {
                return;
            }

            AnalyzeReference(context, invocation);
        }

        public void AnalyzeMethodReference(OperationAnalysisContext context)
        {
            var reference = (IMethodReferenceOperation)context.Operation;
            IMethodSymbol method = reference.Method;

            if (!method.ContainsAttribute(PlaceholderAttribute, ReferenceAttribute))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    PlaceholderAnalyzer.InvalidContextRule,
                    reference.Syntax.GetLocation(),
                    method.Name));
        }

        public void AnalyzePropertyReference(OperationAnalysisContext context)
        {
            var reference = (IPropertyReferenceOperation)context.Operation;
            IMethodSymbol? getter = reference.Property.GetMethod;

            if (getter is null || !getter.ContainsAttribute(ReferenceAttribute))
            {
                return;
            }

            AnalyzeReference(context, reference);
        }

        public void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            var operation = (ITypeOfOperation)context.Operation;
            var placeholders = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            SearchDirectPlaceholders(operation.TypeOperand, placeholders);

            foreach (var placeholder in placeholders)
            {
                var type = placeholder.GetReferenceType(ReferenceAttribute);

                if (type is not ReferenceType.Generic)
                {
                    goto Report;
                }

                var current = operation.Parent;

                while (true)
                {
                    if (current is null)
                    {
                        goto Report;
                    }

                    if (current is not IArgumentOperation argument)
                    {
                        current = current.Parent;
                        continue;
                    }

                    if (argument.Parent is not IInvocationOperation invocation
                        || !invocation.TargetMethod.ContainsAttribute(ReferenceAttribute))
                    {
                        goto Report;
                    }

                    break;
                }

                continue;
            Report:
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        PlaceholderAnalyzer.ReferenceEscapeRule,
                        operation.Syntax.GetLocation()));
            }
        }

        public void AnalyzeVariableDeclarator(OperationAnalysisContext context)
        {
            var declarator = (IVariableDeclaratorOperation)context.Operation;
            var placeholders = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            SearchDirectPlaceholders(declarator.Symbol.Type, placeholders);

            foreach (var placeholder in placeholders)
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(
                        PlaceholderAnalyzer.PlaceholderTypeRule,
                        declarator.Syntax.GetLocation(),
                        declarator.Symbol.Name,
                        placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
            }
        }


        private void AnalyzeReference(OperationAnalysisContext context, IOperation operation)
        {
            IOperation root = operation;
            while (root.Parent is IConversionOperation conversion)
            {
                root = conversion;
            }

            if (root.Parent is IInvocationOperation invocation
                && root == invocation.Instance
                && invocation.TargetMethod.ContainsAttribute(ReferenceAttribute))
            {
                return;
            }

            if (root.Parent is IPropertyReferenceOperation reference
                && root == reference.Instance
                && reference.Property.GetMethod is IMethodSymbol getter
                && getter.ContainsAttribute(ReferenceAttribute))
            {
                return;
            }

            if (root.Parent is IArgumentOperation argument
                && argument.Parent is IInvocationOperation parentInvocation
                && parentInvocation.TargetMethod.ContainsAttribute(PlaceholderAttribute, ReferenceAttribute))
            {
                return;
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    PlaceholderAnalyzer.ReferenceEscapeRule,
                    operation.Syntax.GetLocation()));
        }

        private void SearchDirectPlaceholders(ITypeSymbol type, HashSet<ITypeSymbol> placeholders)
        {
            switch (type)
            {
                case IArrayTypeSymbol arrayType:
                    SearchDirectPlaceholders(arrayType.ElementType, placeholders);
                    break;
                case IFunctionPointerTypeSymbol functionPointer:
                    var signature = functionPointer.Signature;
                    SearchDirectPlaceholders(signature.ReturnType, placeholders);

                    var parameters = signature.Parameters;

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        SearchDirectPlaceholders(parameters[i].Type, placeholders);
                    }

                    break;
                case IPointerTypeSymbol pointerType:
                    SearchDirectPlaceholders(pointerType.PointedAtType, placeholders);
                    break;
                case INamedTypeSymbol namedType:
                    if (namedType.ContainsAttribute(ReferenceAttribute))
                    {
                        placeholders.Add(namedType);
                    }

                    var typeArguments = namedType.TypeArguments;

                    for (int i = 0; i < typeArguments.Length; i++)
                    {
                        SearchDirectPlaceholders(typeArguments[i], placeholders);
                    }

                    break;
                default:
                    break;
            }
        }
    }
}