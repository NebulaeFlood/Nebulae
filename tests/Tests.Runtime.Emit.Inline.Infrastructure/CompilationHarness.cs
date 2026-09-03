using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Nebulae.Runtime.Emit.Inline;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Tests.Runtime.Emit.Inline.Infrastructure;

public sealed record CompilationArtifact(
    string AssemblyPath,
    string? PdbPath,
    ImmutableArray<string> ReferencePaths,
    CSharpCompilation Compilation);

public sealed class CompilationFailureException(string message) : InvalidOperationException(message);

public static class CompilationHarness
{
    public static ImmutableArray<string> DefaultReferencePaths => ReferencePaths;

    public static CSharpCompilation CreateCompilation(
        string source,
        string assemblyName,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        OptimizationLevel optimizationLevel = OptimizationLevel.Release,
        string sourcePath = "Scenario.cs",
        IEnumerable<string>? additionalReferencePaths = null)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            source,
            new CSharpParseOptions(LanguageVersion.Preview),
            sourcePath,
            Encoding.UTF8);

        ImmutableArray<string> referencePaths = GetReferencePaths(additionalReferencePaths);
        ImmutableArray<MetadataReference> metadataReferences = additionalReferencePaths is null
            ? MetadataReferences
            : [.. referencePaths.Select(static path =>
                (MetadataReference)MetadataReference.CreateFromFile(path))];

        return CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            metadataReferences,
            new CSharpCompilationOptions(
                outputKind,
                optimizationLevel: optimizationLevel,
                allowUnsafe: true,
                deterministic: true,
                nullableContextOptions: NullableContextOptions.Enable));
    }

    public static CompilationArtifact Compile(
        string source,
        string assemblyName,
        string outputDirectory,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        OptimizationLevel optimizationLevel = OptimizationLevel.Release,
        bool emitPdb = false,
        string sourcePath = "Scenario.cs",
        IEnumerable<string>? additionalReferencePaths = null,
        DebugInformationFormat debugInformationFormat = DebugInformationFormat.PortablePdb)
    {
        Directory.CreateDirectory(outputDirectory);
        CSharpCompilation compilation = CreateCompilation(
            source,
            assemblyName,
            outputKind,
            optimizationLevel,
            sourcePath,
            additionalReferencePaths);
        ImmutableArray<string> referencePaths = GetReferencePaths(additionalReferencePaths);

        string assemblyPath = System.IO.Path.Combine(outputDirectory, $"{assemblyName}.dll");
        string? pdbPath = emitPdb && debugInformationFormat is not DebugInformationFormat.Embedded
            ? System.IO.Path.Combine(outputDirectory, $"{assemblyName}.pdb")
            : null;

        using var assemblyStream = File.Create(assemblyPath);
        using FileStream? pdbStream = pdbPath is null ? null : File.Create(pdbPath);
        EmitResult result = compilation.Emit(
            assemblyStream,
            pdbStream,
            options: !emitPdb
                ? null
                : new EmitOptions(debugInformationFormat: debugInformationFormat));

        if (!result.Success)
        {
            string diagnostics = string.Join(
                Environment.NewLine,
                result.Diagnostics
                    .Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error)
                    .Select(static diagnostic => diagnostic.ToString()));

            throw new CompilationFailureException(
                $"Compilation '{assemblyName}' failed:{Environment.NewLine}{diagnostics}");
        }

        return new CompilationArtifact(
            assemblyPath,
            pdbPath,
            referencePaths,
            compilation);
    }

    public static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(
        string source,
        DiagnosticAnalyzer analyzer,
        string assemblyName = "AnalyzerScenario")
    {
        CSharpCompilation compilation = CreateCompilation(source, assemblyName);
        ImmutableArray<Diagnostic> compilerErrors = [.. compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error)];

        if (!compilerErrors.IsEmpty)
        {
            throw new CompilationFailureException(
                $"Analyzer source failed to compile:{Environment.NewLine}" +
                string.Join(Environment.NewLine, compilerErrors));
        }

        ImmutableArray<Diagnostic> diagnostics = await compilation
            .WithAnalyzers([analyzer])
            .GetAnalyzerDiagnosticsAsync()
            .ConfigureAwait(false);

        return [.. diagnostics
            .OrderBy(static diagnostic => diagnostic.Location.SourceSpan.Start)
            .ThenBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)];
    }

    private static ImmutableArray<string> ReferencePaths { get; } = GetReferencePaths();

    private static ImmutableArray<MetadataReference> MetadataReferences { get; } =
        [.. ReferencePaths.Select(static path => (MetadataReference)MetadataReference.CreateFromFile(path))];

    private static ImmutableArray<string> GetReferencePaths()
    {
        string? trustedPlatformAssemblies =
            AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;

        if (string.IsNullOrWhiteSpace(trustedPlatformAssemblies))
        {
            throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        }

        var paths = trustedPlatformAssemblies
            .Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Append(typeof(IL).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<string>(paths.Length);

        foreach (string path in paths)
        {
            string identity = AssemblyName.GetAssemblyName(path).FullName
                ?? throw new InvalidOperationException($"Assembly '{path}' has no full identity.");

            if (identities.Add(identity))
            {
                result.Add(path);
            }
        }

        return result.ToImmutable();
    }

    private static ImmutableArray<string> GetReferencePaths(
        IEnumerable<string>? additionalReferencePaths)
    {
        if (additionalReferencePaths is null)
        {
            return ReferencePaths;
        }

        var identities = new HashSet<string>(StringComparer.Ordinal);
        var result = ImmutableArray.CreateBuilder<string>();

        foreach (string path in ReferencePaths.Concat(additionalReferencePaths))
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            string identity = AssemblyName.GetAssemblyName(fullPath).FullName
                ?? throw new InvalidOperationException(
                    $"Assembly '{fullPath}' has no full identity.");

            if (identities.Add(identity))
            {
                result.Add(fullPath);
            }
        }

        return result.ToImmutable();
    }
}
