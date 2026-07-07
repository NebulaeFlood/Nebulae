using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nebulae.Runtime.Emit.Inline.Analyzers;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class DiagnosticCoverageTests
{
    [TestMethod]
    public async Task EverySupportedDiagnosticIdHasAnInvalidUsageExample()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static object Escape() => IL.Ref(typeof(string));

                public static TypeRef Expose(TypeRef value) => value;

                public static void Use()
                {
                    Consume(IL.Emit.Nop);
                }

                private static void Consume(Action action) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);
        string[] supportedIds = new PlaceholderAnalyzer().SupportedDiagnostics
            .Select(static descriptor => descriptor.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();
        string[] actualIds = diagnostics
            .Select(static diagnostic => diagnostic.Id)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        Assert.HasCount(3, diagnostics);
        CollectionAssert.AreEqual(supportedIds, actualIds);
        Assert.AreEqual("IL.Ref(typeof(string))", GetDiagnosticSnippet(source, diagnostics, "NEBIL001"));
        Assert.AreEqual("Expose", GetDiagnosticSnippet(source, diagnostics, "NEBIL002"));
        Assert.AreEqual("IL.Emit.Nop", GetDiagnosticSnippet(source, diagnostics, "NEBIL003"));
    }

    private static string GetDiagnosticSnippet(string source, IEnumerable<Diagnostic> diagnostics, string id)
    {
        Diagnostic diagnostic = diagnostics.Single(diagnostic => diagnostic.Id == id);
        Assert.AreEqual(id, diagnostic.Id);
        return AnalyzerTestHelpers.GetSourceSnippet(source, diagnostic);
    }
}
