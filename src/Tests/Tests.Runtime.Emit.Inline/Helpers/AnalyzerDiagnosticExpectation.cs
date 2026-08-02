using Microsoft.CodeAnalysis;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal sealed record AnalyzerDiagnosticExpectation(
    string Id,
    string? SourceSnippet = null,
    int SourceOccurrence = 0)
{
    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Error;

    public string? Message { get; init; }

    public string? SourcePath { get; init; }

    public AnalyzerDiagnosticLocationExpectation[] AdditionalLocations { get; init; } = [];
}

internal sealed record AnalyzerTestSource(string Text, string Path);

internal readonly record struct AnalyzerDiagnosticLocationExpectation(
    string SourceSnippet,
    int SourceOccurrence = 0);
