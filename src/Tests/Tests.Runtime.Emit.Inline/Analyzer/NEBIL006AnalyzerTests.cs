using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL006AnalyzerTests
{
    [TestMethod]
    public async Task DuplicateDefinitionsReportEveryDefinitionAfterTheFirst()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Label("a");
                    IL.Label("a");
                    IL.Label("a");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL006", "\"a\"", 1) with
            {
                AdditionalLocations = [new("\"a\"")]
            },
            AnalyzerTestHelpers.Diagnostic("NEBIL006", "\"a\"", 2) with
            {
                AdditionalLocations = [new("\"a\"")]
            });
    }

    [TestMethod]
    public async Task SameLabelCanBeDefinedInDifferentMethodsAndLocalFunctions()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void First()
                {
                    IL.Label("shared");

                    static void Local()
                    {
                        IL.Label("shared");
                    }

                    Local();
                }

                public static void Second()
                {
                    IL.Label("shared");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task DuplicateDefinitionsInsideLocalFunctionAreReported()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    static void Local()
                    {
                        IL.Label("local");
                        IL.Label("local");
                    }

                    Local();
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL006", "\"local\"", 1) with
            {
                AdditionalLocations = [new("\"local\"")]
            });
    }

    [TestMethod]
    public async Task LabelNamesAreCaseSensitive()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Label("Target");
                    IL.Label("target");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }
}
