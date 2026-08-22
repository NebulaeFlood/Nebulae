using Microsoft.CodeAnalysis;
using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class LabelAnalyzerContractTests
{
    [TestMethod]
    public async Task Label_WhenDefinedTwiceInOneFunction_ReportsNEBIL5001_WithFirstDefinition()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Label("same");
                    IL.Label("same");
                }
            }
            """;
        int first = source.IndexOf("\"same\"", StringComparison.Ordinal);
        int second = source.IndexOf("\"same\"", first + 1, StringComparison.Ordinal);

        Diagnostic diagnostic = await GetSingleDiagnosticAsync(source, "NEBIL5001");

        Assert.AreEqual(second, diagnostic.Location.SourceSpan.Start);
        Assert.AreEqual("\"same\"".Length, diagnostic.Location.SourceSpan.Length);
        Assert.HasCount(1, diagnostic.AdditionalLocations);
        Assert.AreEqual(first, diagnostic.AdditionalLocations[0].SourceSpan.Start);
        Assert.AreEqual("\"same\"".Length, diagnostic.AdditionalLocations[0].SourceSpan.Length);
    }

    [TestMethod]
    public async Task Branch_WhenTargetIsNotDefinedInFunction_ReportsNEBIL5002_OnLabel()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Br("missing");
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL5002", "\"missing\"");
    }
}
