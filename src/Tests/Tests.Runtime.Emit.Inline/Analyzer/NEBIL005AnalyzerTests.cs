using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL005AnalyzerTests
{
    [TestMethod]
    public async Task LegalOperandValuesProduceNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Unaligned(1);
                    IL.Emit.Unaligned(2);
                    IL.Emit.Unaligned(4);
                    IL.Emit.No(1);
                    IL.Emit.No(7);
                    IL.Emit.Ldstr("");
                    IL.Emit.Br("a");
                    IL.Emit.Switch("a", nameof(Example));
                    IL.Label("a");
                    IL.Label(nameof(Example));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task InvalidOperandValuesProduceInvalidValueDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Target
            {
                public void Method() { }
            }

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Unaligned(0);
                    IL.Emit.Unaligned(3);
                    IL.Emit.Unaligned(8);
                    IL.Emit.No(0);
                    IL.Emit.No(8);
                    IL.Emit.No(255);
                    IL.Emit.Ldstr(null!);
                    IL.Label(null!);
                    IL.Label("");
                    IL.Emit.Br(null!);
                    IL.Emit.Br("");
                    IL.Emit.Switch("a", "", null!);
                    IL.Label("a");
                    IL.Emit.Call(IL.Ref(typeof(Target)).Method(""));
                    IL.Emit.Call(IL.Ref(typeof(Target)).Method(null!));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            Enumerable.Repeat(AnalyzerTestHelpers.Diagnostic("NEBIL005"), 15).ToArray());
    }
}
