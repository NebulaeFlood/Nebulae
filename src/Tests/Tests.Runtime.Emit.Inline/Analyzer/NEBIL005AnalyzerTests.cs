using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL005AnalyzerTests
{
    [TestMethod]
    public async Task LegalOperandValue_ProducesNoDiagnostics()
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
    [DataRow("IL.Emit.Unaligned(0);", "0", "Unaligned")]
    [DataRow("IL.Emit.Unaligned(3);", "3", "Unaligned")]
    [DataRow("IL.Emit.Unaligned(8);", "8", "Unaligned")]
    [DataRow("IL.Emit.No(0);", "0", "No")]
    [DataRow("IL.Emit.No(8);", "8", "No")]
    [DataRow("IL.Emit.No(255);", "255", "No")]
    [DataRow("IL.Emit.Ldstr(null!);", "null", "Ldstr")]
    [DataRow("IL.Label(null!);", "null", "Label")]
    [DataRow("IL.Label(\"\");", "\"\"", "Label")]
    [DataRow("IL.Emit.Br(null!);", "null", "Br")]
    [DataRow("IL.Emit.Br(\"\");", "\"\"", "Br")]
    [DataRow("IL.Emit.Switch(\"\");", "\"\"", "Switch")]
    [DataRow("IL.Emit.Switch(null!);", "null", "Switch")]
    [DataRow("IL.Emit.Call(IL.Ref(typeof(Target)).Method(\"\"));", "\"\"", "Method")]
    [DataRow("IL.Emit.Call(IL.Ref(typeof(Target)).Method(null!));", "null", "Method")]
    public async Task InvalidOperandValue_ReportsDiagnostic(
        string statement,
        string sourceSnippet,
        string memberName)
    {
        string source = $$"""
            using Nebulae.Runtime.Emit.Inline;

            sealed class Target
            {
                public void Method() { }
            }

            static class Example
            {
                public static void Use()
                {
                    {{statement}}
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL005", sourceSnippet) with
            {
                Message = $"Operand for inline IL placeholder member '{memberName}' has an invalid value",
            });
    }
}
