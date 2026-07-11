using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Nebulae.Runtime.Emit.Inline.Analyzers.Helpers;
using System;
using System.Collections.Generic;

namespace Nebulae.Runtime.Emit.Inline.Analyzers
{
    internal sealed class LabelAnalyzerState(
        IMethodSymbol owningMethod,
        INamedTypeSymbol placeholderAttribute,
        INamedTypeSymbol? expressionType)
    {
        //------------------------------------------------------
        //
        //  Public Fields
        //
        //------------------------------------------------------

        #region Public Fields

        public readonly IMethodSymbol OwningMethod = owningMethod;
        public readonly INamedTypeSymbol PlaceholderAttribute = placeholderAttribute;
        public readonly INamedTypeSymbol? ExpressionType = expressionType;

        #endregion


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods

        public void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            IMethodSymbol method = invocation.TargetMethod;
            IMethodSymbol? containingMethod = invocation.GetContainingMethod(context.Compilation);

            if (containingMethod is null
                || !containingMethod.IsInside(OwningMethod)
                || !method.ContainsAttribute(PlaceholderAttribute)
                || invocation.IsInside(ExpressionType))
            {
                return;
            }

            PlaceholderInfo placeholder = method.GetPlaceholderInfo(PlaceholderAttribute);
            LabelScope scope = GetScope(containingMethod);

            if (placeholder.Code is PlaceholderCode.Label)
            {
                AnalyzeDefinition(context, invocation, scope);
                return;
            }

            switch (placeholder.Operand)
            {
                case PlaceholderOperand.Branch:
                    AnalyzeBranch(invocation, scope);
                    break;
                case PlaceholderOperand.Branches:
                    AnalyzeBranches(invocation, scope);
                    break;
            }
        }

        public void Complete(OperationBlockAnalysisContext context)
        {
            foreach (LabelScope scope in _scopes.Values)
            {
                foreach (LabelReference reference in scope.References)
                {
                    if (!scope.Definitions.ContainsKey(reference.Name))
                    {
                        context.ReportDiagnostic(
                            PlaceholderAnalyzer.UndefinedLabelRule,
                            reference.Location,
                            reference.Name);
                    }
                }
            }
        }

        #endregion


        //------------------------------------------------------
        //
        //  Private Methods
        //
        //------------------------------------------------------

        #region Private Methods

        private static void AnalyzeBranch(IInvocationOperation invocation, LabelScope scope)
        {
            if (invocation.Arguments.Length is 0)
            {
                return;
            }

            RegisterReference(invocation.Arguments[0].Value, scope);
        }

        private static void AnalyzeBranches(IInvocationOperation invocation, LabelScope scope)
        {
            var arguments = invocation.Arguments;

            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i].Value.VisitArrayItems(operation => RegisterReference(operation, scope));
            }
        }

        private static void AnalyzeDefinition(
            OperationAnalysisContext context,
            IInvocationOperation invocation,
            LabelScope scope)
        {
            if (invocation.Arguments.Length is 0)
            {
                return;
            }

            IOperation value = invocation.Arguments[0].Value;

            if (!value.TryGetValidLabel(out string label))
            {
                return;
            }

            Location location = value.Syntax.GetLocation();

            if (scope.Definitions.TryGetValue(label, out Location? firstLocation))
            {
                context.ReportDiagnostic(
                    PlaceholderAnalyzer.DuplicateLabelRule,
                    location,
                    [firstLocation],
                    label);
            }
            else
            {
                scope.Definitions.Add(label, location);
            }
        }

        private LabelScope GetScope(IMethodSymbol method)
        {
            if (!_scopes.TryGetValue(method, out LabelScope? scope))
            {
                scope = new LabelScope();
                _scopes.Add(method, scope);
            }

            return scope;
        }

        private static void RegisterReference(IOperation operation, LabelScope scope)
        {
            if (operation.TryGetValidLabel(out string label))
            {
                scope.References.Add(new LabelReference(label, operation.Syntax.GetLocation()));
            }
        }

        #endregion


        private readonly Dictionary<IMethodSymbol, LabelScope> _scopes = new(SymbolEqualityComparer.Default);


        private sealed class LabelScope
        {
            public readonly Dictionary<string, Location> Definitions = new(StringComparer.Ordinal);

            public readonly List<LabelReference> References = [];
        }


        private readonly struct LabelReference(string name, Location location)
        {
            public readonly string Name = name;

            public readonly Location Location = location;
        }
    }
}
