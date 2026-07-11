using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Nebulae.Runtime.Emit.Inline.Analyzers.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal sealed class PlaceholderAnalyzerState(
        INamedTypeSymbol placeholderAttribute,
        INamedTypeSymbol referenceAttribute,
        IAssemblySymbol placeholderAssembly,
        INamedTypeSymbol? expressionType,
        INamedTypeSymbol? systemType)
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        public readonly INamedTypeSymbol PlaceholderAttribute = placeholderAttribute;
        public readonly INamedTypeSymbol ReferenceAttribute = referenceAttribute;
        public readonly IAssemblySymbol PlaceholderAssembly = placeholderAssembly;
        public readonly INamedTypeSymbol? ExpressionType = expressionType;

        public readonly INamedTypeSymbol? SystemType = systemType;

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

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
                        PlaceholderAnalyzer.PlaceholderTypeRule,
                        location,
                        symbol.Name,
                        placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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
                        PlaceholderAnalyzer.InvalidContextRule,
                        invocation,
                        method.Name);

                    return;
                }

                AnalyzeConstantParameter(context, invocation, method.GetPlaceholderInfo(PlaceholderAttribute));
                return;
            }

            if (!method.ContainsAttribute(ReferenceAttribute))
            {
                return;
            }

            AnalyzeConstantParameters(context, invocation);
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
                PlaceholderAnalyzer.InvalidContextRule,
                reference,
                method.Name);
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
                ReferenceType type = placeholder.GetReferenceType(ReferenceAttribute);

                if (type is not ReferenceType.Generic)
                {
                    goto Report;
                }

                IOperation? current = operation.Parent;

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
                context.ReportDiagnostic(PlaceholderAnalyzer.ReferenceEscapeRule, operation);
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
                    PlaceholderAnalyzer.PlaceholderTypeRule,
                    declarator,
                    declarator.Symbol.Name,
                    placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
        }

        public void AnalyzeOperationBlockStart(OperationBlockStartAnalysisContext context)
        {
            if (context.OwningSymbol is not IMethodSymbol symbol)
            {
                return;
            }

            var state = new LabelAnalyzerState(
                symbol,
                PlaceholderAttribute,
                ExpressionType);

            context.RegisterOperationAction(state.AnalyzeInvocation, OperationKind.Invocation);
            context.RegisterOperationBlockEndAction(state.Complete);
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Static Methods
        //
        //------------------------------------------------------

        #region Private Static Methods

        private static void AnalyzeConstantValue(
            OperationAnalysisContext context,
            IOperation operation,
            IMethodSymbol method,
            PlaceholderCode code,
            PlaceholderOperand operand)
        {
            switch (code)
            {
                case PlaceholderCode.Unaligned:
                    byte value = (byte)operation.ConstantValue.Value!;

                    if (value is not 1 and not 2 and not 4)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.InvalidOperandValueRule,
                            operation,
                            method.Name);
                    }
                    return;
                case PlaceholderCode.No:
                    value = (byte)operation.ConstantValue.Value!;

                    if (value < 1 || value > 7)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.InvalidOperandValueRule,
                            operation,
                            method.Name);
                    }
                    return;
            }

            switch (operand)
            {
                case PlaceholderOperand.String:
                    AnalyzeConstantString(context, operation, method, allowEmpty: code is not PlaceholderCode.Label);
                    return;
                case PlaceholderOperand.Branch:
                    AnalyzeConstantString(context, operation, method, allowEmpty: false);
                    return;
            }
        }

        private static void AnalyzeConstantString(
            OperationAnalysisContext context,
            IOperation operation,
            IMethodSymbol method,
            bool allowEmpty)
        {
            if (operation.ConstantValue.Value is not string value || (!allowEmpty && value.Length is 0))
            {
                context.ReportDiagnostic(
                    PlaceholderAnalyzer.InvalidOperandValueRule,
                    operation,
                    method.Name);
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private void AnalyzeArrayItems(
            OperationAnalysisContext context,
            IOperation operation,
            IMethodSymbol method,
            Func<IOperation, bool> isConstant,
            Action<IOperation>? analyzeValue = null)
        {
            operation = operation.GetInnermostConversion();

            if (isConstant(operation))
            {
                analyzeValue?.Invoke(operation);
                return;
            }

            if (operation.VisitArrayItems(AnalyzeItem))
            {
                return;
            }

            context.ReportDiagnostic(
                PlaceholderAnalyzer.NonConstantOperandRule,
                operation,
                method.Name);

            void AnalyzeItem(IOperation value)
            {
                if (!isConstant(value))
                {
                    context.ReportDiagnostic(
                        PlaceholderAnalyzer.NonConstantOperandRule,
                        value,
                        method.Name);
                }
                else
                {
                    analyzeValue?.Invoke(value);
                }
            }
        }

        private void AnalyzeConstantParameter(
            OperationAnalysisContext context,
            IInvocationOperation invocation,
            PlaceholderInfo placeholder)
        {
            switch (placeholder.Operand)
            {
                case PlaceholderOperand.Byte:
                case PlaceholderOperand.Int32:
                case PlaceholderOperand.Int64:
                case PlaceholderOperand.Single:
                case PlaceholderOperand.Double:
                case PlaceholderOperand.String:
                case PlaceholderOperand.Branch:
                    if (invocation.Arguments.Length is 0)
                    {
                        return;
                    }

                    var argument = invocation.Arguments[0];

                    if (!argument.Value.ConstantValue.HasValue)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.NonConstantOperandRule,
                            argument.Value,
                            invocation.TargetMethod.Name);
                    }
                    else
                    {
                        AnalyzeConstantValue(
                            context,
                            argument.Value,
                            invocation.TargetMethod,
                            placeholder.Code,
                            placeholder.Operand);
                    }
                    break;
                case PlaceholderOperand.Branches:
                    var arguments = invocation.Arguments;

                    for (int i = 0; i < arguments.Length; i++)
                    {
                        AnalyzeArrayItems(
                            context,
                            arguments[i].Value,
                            invocation.TargetMethod,
                            static value => value.ConstantValue.HasValue,
                            value => AnalyzeConstantString(context, value, invocation.TargetMethod, allowEmpty: false));
                    }
                    break;
                case PlaceholderOperand.TypeRef:
                    AnalyzeConstantParameters(context, invocation);
                    break;
            }
        }

        private void AnalyzeConstantParameters(OperationAnalysisContext context, IInvocationOperation invocation)
        {
            var arguments = invocation.Arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                var parameterType = argument.Parameter?.Type;

                if (parameterType?.SpecialType is SpecialType.System_String)
                {
                    if (!argument.Value.ConstantValue.HasValue)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.NonConstantOperandRule,
                            argument.Value,
                            invocation.TargetMethod.Name);
                    }
                    else
                    {
                        AnalyzeConstantString(context, argument.Value, invocation.TargetMethod, allowEmpty: false);
                    }
                }
                else if (parameterType is IArrayTypeSymbol { ElementType.SpecialType: SpecialType.System_String })
                {
                    AnalyzeArrayItems(
                        context,
                        argument.Value,
                        invocation.TargetMethod,
                        static value => value.ConstantValue.HasValue,
                        value => AnalyzeConstantString(context, value, invocation.TargetMethod, allowEmpty: false));
                }
                else if (SymbolEqualityComparer.Default.Equals(parameterType, SystemType))
                {
                    if (!argument.Value.IsTypeOf())
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.NonConstantOperandRule,
                            argument.Value,
                            invocation.TargetMethod.Name);
                    }
                }
                else if (parameterType is IArrayTypeSymbol arrayType
                    && SymbolEqualityComparer.Default.Equals(arrayType.ElementType, SystemType))
                {
                    AnalyzeArrayItems(
                        context,
                        argument.Value,
                        invocation.TargetMethod,
                        value => value.IsTypeOf() || value.IsTypeEmptyTypes(SystemType));
                }
            }
        }

        private void AnalyzeReference(OperationAnalysisContext context, IOperation operation)
        {
            IOperation root = operation.GetOutermostConversion();

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

            context.ReportDiagnostic(PlaceholderAnalyzer.ReferenceEscapeRule, operation);
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

        #endregion
    }
}
