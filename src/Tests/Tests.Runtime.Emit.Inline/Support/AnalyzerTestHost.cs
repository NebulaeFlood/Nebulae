using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline.Analyzers;

namespace Tests.Runtime.Emit.Inline.Support;

internal static class AnalyzerTestHost
{
    private const string InlineAssemblyFileName = "Nebulae.Runtime.Emit.Inline.dll";

    public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
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

    public static string GetSourceSnippet(string source, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(diagnostic);

        return source.Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length);
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
