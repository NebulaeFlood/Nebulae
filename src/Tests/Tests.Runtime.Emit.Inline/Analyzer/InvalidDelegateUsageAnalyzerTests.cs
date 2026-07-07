using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class InvalidDelegateUsageAnalyzerTests
{
    [TestMethod]
    public async Task PassingAPlaceholderMethodGroupProducesInvalidContextDiagnostic()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    Consume(IL.Emit.Nop);
                }

                private static void Consume(Action action) { }
            }
            """;

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL003", diagnostics[0].Id);
        Assert.AreEqual("IL.Emit.Nop", AnalyzerTestHelpers.GetSourceSnippet(source, diagnostics[0]));
    }
}
