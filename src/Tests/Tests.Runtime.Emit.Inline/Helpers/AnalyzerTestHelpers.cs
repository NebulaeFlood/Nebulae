using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Nebulae.Runtime.Emit.Inline.Analyzers;
using System.Collections.Immutable;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal static class AnalyzerTestHelpers
{
    private const string InlineAssemblyFileName = "Nebulae.Runtime.Emit.Inline.dll";

    public static AnalyzerDiagnosticExpectation Diagnostic(
        string id,
        string? sourceSnippet = null,
        int sourceOccurrence = 0)
    {
        return new AnalyzerDiagnosticExpectation(id, sourceSnippet, sourceOccurrence);
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Latest));

        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "AnalyzerTestAssembly",
            syntaxTrees: [syntaxTree],
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        Diagnostic[] compilationErrors = compilation.GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        if (compilationErrors.Length != 0)
        {
            Assert.Fail($"Analyzer test source failed to compile:{Environment.NewLine}{string.Join(Environment.NewLine, compilationErrors.Select(static diagnostic => diagnostic.ToString()))}");
        }

        return await compilation
            .WithAnalyzers([new PlaceholderAnalyzer()])
            .GetAnalyzerDiagnosticsAsync();
    }

    public static async Task<ImmutableArray<Diagnostic>> VerifyDiagnosticsAsync(
        string source,
        params AnalyzerDiagnosticExpectation[] expectations)
    {
        ArgumentNullException.ThrowIfNull(expectations);

        ImmutableArray<Diagnostic> diagnostics = await GetDiagnosticsAsync(source);
        List<Diagnostic> unmatchedDiagnostics = [.. diagnostics];
        Assert.HasCount(
            expectations.Length,
            unmatchedDiagnostics,
            $"Actual diagnostics:{Environment.NewLine}{FormatDiagnostics(unmatchedDiagnostics)}");

        foreach (AnalyzerDiagnosticExpectation expectation in expectations)
        {
            Diagnostic diagnostic = FindAndRemoveDiagnostic(source, unmatchedDiagnostics, expectation);

            Assert.AreEqual(expectation.Severity, diagnostic.Severity, $"Unexpected severity for {expectation.Id}.");

            Assert.HasCount(
                expectation.AdditionalLocations.Length,
                diagnostic.AdditionalLocations,
                $"Unexpected additional locations for {expectation.Id}.");

            for (int i = 0; i < expectation.AdditionalLocations.Length; i++)
            {
                AnalyzerDiagnosticLocationExpectation location = expectation.AdditionalLocations[i];
                Assert.AreEqual(
                    FindSourceSpan(source, location.SourceSnippet, location.SourceOccurrence),
                    diagnostic.AdditionalLocations[i].SourceSpan,
                    $"Unexpected additional location {i} for {expectation.Id}.");
            }
        }

        Assert.IsEmpty(unmatchedDiagnostics, $"Unmatched diagnostics:{Environment.NewLine}{FormatDiagnostics(unmatchedDiagnostics)}");
        return diagnostics;
    }

    public static Task<ImmutableArray<Diagnostic>> VerifyNoDiagnosticsAsync(string source)
    {
        return VerifyDiagnosticsAsync(source);
    }

    private static Diagnostic FindAndRemoveDiagnostic(
        string source,
        List<Diagnostic> diagnostics,
        AnalyzerDiagnosticExpectation expectation)
    {
        TextSpan? expectedSpan = expectation.SourceSnippet is null
            ? null
            : FindSourceSpan(source, expectation.SourceSnippet, expectation.SourceOccurrence);

        int index = diagnostics.FindIndex(diagnostic =>
            diagnostic.Id == expectation.Id
            && (expectedSpan is null || diagnostic.Location.SourceSpan == expectedSpan.Value));

        if (index < 0)
        {
            Assert.Fail(
                $"Expected diagnostic {expectation.Id}"
                + (expectedSpan is null ? string.Empty : $" at {expectedSpan}")
                + $" was not found.{Environment.NewLine}Actual diagnostics:{Environment.NewLine}{FormatDiagnostics(diagnostics)}");
        }

        Diagnostic result = diagnostics[index];
        diagnostics.RemoveAt(index);
        return result;
    }

    private static TextSpan FindSourceSpan(string source, string snippet, int occurrence)
    {
        if (occurrence < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(occurrence));
        }

        int start = -1;

        for (int i = 0; i <= occurrence; i++)
        {
            start = source.IndexOf(snippet, start + 1, StringComparison.Ordinal);

            if (start < 0)
            {
                Assert.Fail($"Source snippet occurrence {occurrence} was not found: {snippet}");
            }
        }

        return new TextSpan(start, snippet.Length);
    }

    private static string FormatDiagnostics(IEnumerable<Diagnostic> diagnostics)
    {
        return string.Join(Environment.NewLine, diagnostics.Select(static diagnostic => diagnostic.ToString()));
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        string? trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (string.IsNullOrEmpty(trustedPlatformAssemblies))
        {
            throw new AssertFailedException("The runtime did not provide trusted platform assemblies.");
        }

        foreach (string path in trustedPlatformAssemblies.Split(Path.PathSeparator))
        {
            yield return MetadataReference.CreateFromFile(path);
        }

        string inlineAssemblyPath = Path.Combine(AppContext.BaseDirectory, InlineAssemblyFileName);

        if (!File.Exists(inlineAssemblyPath))
        {
            throw new AssertFailedException($"Inline IL assembly was not found at '{inlineAssemblyPath}'.");
        }

        yield return MetadataReference.CreateFromFile(inlineAssemblyPath);
    }
}
