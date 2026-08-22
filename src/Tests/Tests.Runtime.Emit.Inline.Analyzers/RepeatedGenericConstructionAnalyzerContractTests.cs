using Microsoft.CodeAnalysis;
using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class RepeatedGenericConstructionAnalyzerContractTests
{
    [TestMethod]
    public async Task GenericMethodReference_WhenConstructedTwice_ReportsNEBIL3002_OnSecondCall()
    {
        const string source = """
            using System;
            using System.Linq;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Enumerable))
                            .Method(nameof(Enumerable.Empty), 1)
                            .MakeGeneric(typeof(string))
                            .MakeGeneric(typeof(string)));
                }
            }
            """;
        int first = source.IndexOf("MakeGeneric", StringComparison.Ordinal);
        int second = source.IndexOf("MakeGeneric", first + 1, StringComparison.Ordinal);

        Diagnostic diagnostic = await GetSingleDiagnosticAsync(source, "NEBIL3002");

        Assert.AreEqual(second, diagnostic.Location.SourceSpan.Start);
        Assert.AreEqual("MakeGeneric".Length, diagnostic.Location.SourceSpan.Length);
    }
}
