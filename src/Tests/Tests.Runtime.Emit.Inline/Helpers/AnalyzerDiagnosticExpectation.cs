using Microsoft.CodeAnalysis;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal sealed record AnalyzerDiagnosticExpectation(
    string id,
    string? sourceSnippet = null,
    int sourceOccurrence = 0)
{
    public string Id { get; } = id;

    public string? SourceSnippet { get; } = sourceSnippet;

    public int SourceOccurrence { get; } = sourceOccurrence;

    public DiagnosticSeverity Severity { get; init; } = DiagnosticSeverity.Error;

    public AnalyzerDiagnosticLocationExpectation[] AdditionalLocations { get; init; } = [];
}

internal readonly record struct AnalyzerDiagnosticLocationExpectation(
    string SourceSnippet,
    int SourceOccurrence = 0);
