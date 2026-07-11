using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL003AnalyzerTests
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

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL003", "IL.Emit.Nop"));
    }

    [TestMethod]
    public async Task PlaceholderCallsProduceNoDiagnosticsInMethodBodies()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Ldc_I4(42);
                    IL.Emit.Pop();
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }
}
