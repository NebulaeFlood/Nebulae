using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Runtime.Emit.Inline.Support;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class InvalidReferenceUsageAnalyzerTests
{
    [TestMethod]
    public async Task ReturningAReferencePlaceholderProducesReferenceEscapeDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static object Escape() => IL.Ref(typeof(string));
            }
            """;

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL001", diagnostics[0].Id);
        Assert.AreEqual("IL.Ref(typeof(string))", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }

    [TestMethod]
    public async Task PassingAReferencePlaceholderToOrdinaryCodeProducesReferenceEscapeDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    Consume(IL.Ref(typeof(string)));
                }

                private static void Consume(object value) { }
            }
            """;

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL001", diagnostics[0].Id);
        Assert.AreEqual("IL.Ref(typeof(string))", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }
}
