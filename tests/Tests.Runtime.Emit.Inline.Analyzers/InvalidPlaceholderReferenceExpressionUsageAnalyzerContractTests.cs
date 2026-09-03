using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class InvalidPlaceholderReferenceExpressionUsageAnalyzerContractTests
{
    [TestMethod]
    public async Task ReferenceExpression_WhenStoredAsObject_ReportsNEBIL3001_OnReferenceExpression()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    object value = IL.Ref(typeof(string));
                    GC.KeepAlive(value);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL3001",
            "IL.Ref(typeof(string))");
    }

    [TestMethod]
    public async Task ReferenceExpression_WhenReturnedAsObject_ReportsNEBIL3001_OnReferenceExpression()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static object Run()
                {
                    return IL.Ref(typeof(string)).Method("Create");
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL3001",
            "IL.Ref(typeof(string)).Method(\"Create\")");
    }

    [TestMethod]
    public async Task ReferenceExpression_WhenPassedToOrdinaryCode_ReportsNEBIL3001_OnReferenceExpression()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    GC.KeepAlive(IL.Ref(typeof(string)).Property("Length"));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL3001",
            "IL.Ref(typeof(string)).Property(\"Length\")");
    }
}
