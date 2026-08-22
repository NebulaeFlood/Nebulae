using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

                    var genericParameters = method.TypeParameters;

                    for (int i = 0; i < genericParameters.Length; i++)
                    {
                        var constraints = genericParameters[i].ConstraintTypes;

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

                    if (type.DelegateInvokeMethod is IMethodSymbol invokeMethod)
                    {
                        SearchDirectPlaceholders(invokeMethod.ReturnType, placeholders);

                        parameters = invokeMethod.Parameters;

                        for (int i = 0; i < parameters.Length; i++)
                        {
                            SearchDirectPlaceholders(parameters[i].Type, placeholders);
                        }
                    }

                    var interfaces = type.Interfaces;

                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        SearchDirectPlaceholders(interfaces[i], placeholders);
                    }

                    genericParameters = type.TypeParameters;

                    for (int i = 0; i < genericParameters.Length; i++)
                    {
                        var constraints = genericParameters[i].ConstraintTypes;

                        for (int j = 0; j < constraints.Length; j++)
                        {
                            SearchDirectPlaceholders(constraints[j], placeholders);
                        }
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
                        PlaceholderAnalyzer.InvalidInstructionUsageRule,
                        invocation,
                        method.Name);

                    return;
                }

                PlaceholderInfo placeholder = method.GetPlaceholderInfo(PlaceholderAttribute);

                AnalyzeConstantParameter(context, invocation, placeholder);
                AnalyzeExtendedInstructionUsage(context, invocation, placeholder);
                AnalyzeVariableOperand(context, invocation, placeholder);
                return;
            }

            if (method.ContainsAttribute(ReferenceAttribute))
            {
                AnalyzeConstantParameters(context, invocation);
                AnalyzePlaceholderReference(context, invocation);
                AnalyzeRepeatedGenericMethodConstruction(context, invocation);
                return;
            }
        }

        public void AnalyzeInstructionMethodReference(OperationAnalysisContext context)
        {
            var reference = (IMethodReferenceOperation)context.Operation;
            IMethodSymbol method = reference.Method;

            if (!method.ContainsAttribute(PlaceholderAttribute))
            {
                return;
            }

            context.ReportDiagnostic(
                PlaceholderAnalyzer.InvalidInstructionUsageRule,
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

            AnalyzePlaceholderReference(context, reference);
        }

        public void AnalyzeTypeOf(OperationAnalysisContext context)
        {
            var operation = (ITypeOfOperation)context.Operation;
            var placeholders = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            SearchDirectPlaceholders(operation.TypeOperand, placeholders);

            if (placeholders.Count is 0)
            {
                return;
            }

            for (IOperation? current = operation.Parent; current is not null; current = current.Parent)
            {
                if (current is not IArgumentOperation argument)
                {
                    continue;
                }

                if (argument.Parent is not IInvocationOperation invocation)
                {
                    goto InvalidUsage;
                }

                switch (invocation.TargetMethod.GetReferenceType(ReferenceAttribute))
                {
                    case ReferenceType.Type:
                    case ReferenceType.Constructor:
                    case ReferenceType.Indexer:
                    case ReferenceType.Method:
                        foreach (var placeholder in placeholders)
                        {
                            if (placeholder.GetReferenceType(ReferenceAttribute) is not ReferenceType.Generic)
                            {
                                context.ReportDiagnostic(
                                    PlaceholderAnalyzer.InvalidPlaceholderUsageRule,
                                    operation,
                                    placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                            }
                        }
                        return;
                    default:
                        goto InvalidUsage;
                }
            }

        InvalidUsage:
            foreach (var placeholder in placeholders)
            {
                context.ReportDiagnostic(
                    PlaceholderAnalyzer.InvalidPlaceholderUsageRule,
                    operation,
                    placeholder.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
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
                            PlaceholderAnalyzer.InvalidConstantValueRule,
                            operation,
                            method.Name);
                    }
                    return;
                case PlaceholderCode.No:
                    value = (byte)operation.ConstantValue.Value!;

                    if (value < 1 || value > 7)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.InvalidConstantValueRule,
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
                    PlaceholderAnalyzer.InvalidConstantValueRule,
                    operation,
                    method.Name);
            }
        }

        private static void AnalyzeExtendedInstructionUsage(
            OperationAnalysisContext context,
            IInvocationOperation invocation,
            PlaceholderInfo placeholder)
        {
            if (placeholder.IsPrimitive)
            {
                return;
            }

            IOperation operation = invocation.GetOutermostConversion();

            switch (placeholder.Code)
            {
                case PlaceholderCode.Fail:
                    if (operation.Parent is IThrowOperation @throw
                        && @throw.Exception == operation)
                    {
                        return;
                    }
                    break;
                case PlaceholderCode.Ret:
                    if (operation.Parent is IReturnOperation @return
                        && @return.Kind is OperationKind.Return
                        && @return.ReturnedValue == operation)
                    {
                        return;
                    }
                    break;
                default:
                    return;
            }

            context.ReportDiagnostic(
                PlaceholderAnalyzer.InvalidExtendedInstructionUsageRule,
                invocation,
                invocation.TargetMethod.Name);
        }

        private static void AnalyzeVariableOperand(
            OperationAnalysisContext context,
            IInvocationOperation invocation,
            PlaceholderInfo placeholder)
        {
            if (placeholder.Operand is not PlaceholderOperand.Argument and not PlaceholderOperand.Variable)
            {
                return;
            }

            if (invocation.Arguments.Length is 0)
            {
                return;
            }

            IOperation operation = invocation.Arguments[0].Value.GetInnermostConversion();

            while (operation is IDeclarationExpressionOperation declaration)
            {
                operation = declaration.Expression.GetInnermostConversion();
            }

            IMethodSymbol? enclosure = invocation.GetEnclosingMethod(context.Compilation);

            if (enclosure is not null)
            {
                switch (placeholder.Operand)
                {
                    case PlaceholderOperand.Argument:
                        if (operation is IParameterReferenceOperation { Parameter: { } parameter }
                            && SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, enclosure))
                        {
                            return;
                        }

                        if (operation is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance }
                            && !enclosure.IsStatic
                            && enclosure.MethodKind is not MethodKind.AnonymousFunction and not MethodKind.LocalFunction)
                        {
                            return;
                        }

                        break;
                    case PlaceholderOperand.Variable:
                        if (operation is ILocalReferenceOperation local
                            && SymbolEqualityComparer.Default.Equals(local.Local.ContainingSymbol, enclosure))
                        {
                            return;
                        }

                        break;
                }
            }

            context.ReportDiagnostic(
                PlaceholderAnalyzer.InvalidVariableOperandRule,
                operation,
                invocation.TargetMethod.Name);
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

        private void AnalyzeConstantParameters(
            OperationAnalysisContext context,
            IInvocationOperation invocation)
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
                else if (parameterType?.SpecialType is SpecialType.System_Int32
                    && argument.Parameter?.Name == "genericParameterCount")
                {
                    if (!argument.Value.ConstantValue.HasValue)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.NonConstantOperandRule,
                            argument.Value,
                            invocation.TargetMethod.Name);
                    }
                    else if (argument.Value.ConstantValue.Value is not int genericParameterCount
                        || genericParameterCount <= 0)
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.InvalidConstantValueRule,
                            argument.Value,
                            invocation.TargetMethod.Name);
                    }
                }
            }
        }

        private void AnalyzePlaceholderReference(
            OperationAnalysisContext context,
            IOperation operation)
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

            switch (operation)
            {
                case IInvocationOperation { TargetMethod.Name: string name }:
                    context.ReportDiagnostic(
                        PlaceholderAnalyzer.InvalidPlaceholderReferenceExpressionUsageRule,
                        operation,
                        name);
                    break;
                case IPropertyReferenceOperation { Property.Name: string name }:
                    context.ReportDiagnostic(
                        PlaceholderAnalyzer.InvalidPlaceholderReferenceExpressionUsageRule,
                        operation,
                        name);
                    break;
            }
        }

        private void AnalyzeRepeatedGenericMethodConstruction(
            OperationAnalysisContext context,
            IInvocationOperation invocation)
        {
            if (invocation.TargetMethod.GetReferenceType(ReferenceAttribute) is not ReferenceType.MethodMakeGeneric)
            {
                return;
            }

            if (invocation.Instance?.GetInnermostConversion() is not IInvocationOperation previousInvocation)
            {
                return;
            }

            if (previousInvocation.TargetMethod.GetReferenceType(ReferenceAttribute) is ReferenceType.Empty or not ReferenceType.MethodMakeGeneric)
            {
                return;
            }

            Location location;

            if (invocation.Syntax is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess })
            {
                location = memberAccess.Name.GetLocation();
            }
            else
            {
                location = invocation.Syntax.GetLocation();
            }

            context.ReportDiagnostic(
                Diagnostic.Create(
                    PlaceholderAnalyzer.RepeatedGenericMethodConstructionRule,
                    location));
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

                    if (namedType.ContainingType is not null)
                    {
                        SearchDirectPlaceholders(namedType.ContainingType, placeholders);
                    }

                    break;
                default:
                    break;
            }
        }

        #endregion
    }
}
