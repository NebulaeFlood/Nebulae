using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;

namespace Nebulae.Runtime.Emit.Inline.Analyzers.Helpers
{
    internal static class DiagnosticReportHelpers
    {
        public static void ReportDiagnostic(
            this in OperationAnalysisContext context,
            DiagnosticDescriptor rule,
            IOperation operation,
            params object?[] messageArgs)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    rule,
                    operation.Syntax.GetLocation(),
                    messageArgs));
        }

        public static void ReportDiagnostic(
            this in OperationAnalysisContext context,
            DiagnosticDescriptor rule,
            Location location,
            IEnumerable<Location>? additionalLocations,
            params object?[] messageArgs)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    rule,
                    location,
                    additionalLocations,
                    messageArgs));
        }

        public static void ReportDiagnostic(
            this in OperationBlockAnalysisContext context,
            DiagnosticDescriptor rule,
            Location location,
            params object?[] messageArgs)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    rule,
                    location,
                    messageArgs));
        }

        public static void ReportDiagnostic(
            this in SymbolAnalysisContext context,
            DiagnosticDescriptor rule,
            Location location,
            params object?[] messageArgs)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    rule,
                    location,
                    messageArgs));
        }
    }
}
