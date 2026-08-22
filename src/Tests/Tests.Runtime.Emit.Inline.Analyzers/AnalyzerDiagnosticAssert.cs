using Microsoft.CodeAnalysis;
using Nebulae.Runtime.Emit.Inline.Analyzers;
using System.Collections.Immutable;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Analyzers;

internal static class AnalyzerDiagnosticAssert
{
    public static async Task AssertSingleDiagnosticAsync(
        string source,
        string id,
        string expectedSourceText)
    {
        Diagnostic diagnostic = await GetSingleDiagnosticAsync(source, id);
        string actualSourceText = GetSourceText(source, diagnostic);

        Assert.AreEqual(expectedSourceText, actualSourceText);
    }

    public static async Task AssertDiagnosticsAsync(
        string source,
        string id,
        params string[] expectedSourceTexts)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        Assert.HasCount(
            expectedSourceTexts.Length,
            diagnostics,
            FormatDiagnostics(diagnostics));

        for (int i = 0; i < diagnostics.Length; i++)
        {
            Diagnostic diagnostic = diagnostics[i];
            AssertDiagnosticContract(diagnostic, id);
            Assert.AreEqual(expectedSourceTexts[i], GetSourceText(source, diagnostic));
        }
    }

    public static async Task AssertNoDiagnosticsAsync(string source)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        Assert.IsEmpty(diagnostics, FormatDiagnostics(diagnostics));
    }

    public static async Task<Diagnostic> GetSingleDiagnosticAsync(string source, string id)
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(source);

        Assert.HasCount(1, diagnostics, FormatDiagnostics(diagnostics));

        Diagnostic diagnostic = diagnostics[0];
        AssertDiagnosticContract(diagnostic, id);
        return diagnostic;
    }

    private static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        return CompilationHarness.AnalyzeAsync(source, new PlaceholderAnalyzer());
    }

    private static void AssertDiagnosticContract(Diagnostic diagnostic, string id)
    {
        Assert.AreEqual(id, diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.IsTrue(diagnostic.Location.IsInSource);
    }

    private static string GetSourceText(string source, Diagnostic diagnostic)
    {
        return source.Substring(
            diagnostic.Location.SourceSpan.Start,
            diagnostic.Location.SourceSpan.Length);
    }

    private static string FormatDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.Select(static diagnostic => diagnostic.ToString()));
    }
}
