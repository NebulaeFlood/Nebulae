using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL007AnalyzerTests
{
    [TestMethod]
    public async Task ForwardBackwardAndSwitchReferencesResolveWithinTheMethod()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Br("forward");
                    IL.Label("backward");
                    IL.Emit.Br("backward");
                    IL.Emit.Switch("switch-a", "missing", "missing");
                    IL.Emit.Switch(new[] { "switch-b" });
                    IL.Emit.Switch(Array.Empty<string>());
                    IL.Label("forward");
                    IL.Label("switch-a");
                    IL.Label("switch-b");
                    IL.Emit.Leave("missing-branch");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"missing\""),
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"missing\"", 1),
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"missing-branch\""));
    }

    [TestMethod]
    public async Task DefinitionInAnotherMethodDoesNotResolveReference()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Define() => IL.Label("shared");
                public static void Use() => IL.Emit.Br("shared");
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"shared\"", 1));
    }

    [TestMethod]
    public async Task DefinitionsAndReferencesResolveInsideEachOwningMethodOrLocalFunction()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void First()
                {
                    IL.Label("shared");
                    IL.Emit.Br("shared");

                    static void Local()
                    {
                        IL.Label("shared");
                        IL.Emit.Br("shared");
                    }

                    Local();
                }

                public static void Second()
                {
                    IL.Label("shared");
                    IL.Emit.Br("shared");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task LocalFunctionAndContainingMethodCannotResolveEachOthersLabels()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    IL.Label("outer");

                    static void Local()
                    {
                        IL.Emit.Br("outer");
                        IL.Label("inner");
                    }

                    IL.Emit.Br("inner");
                    Local();
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"outer\"", 1),
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"inner\"", 1));
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
                    IL.Emit.Br("target");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL007", "\"target\""));
    }
}
