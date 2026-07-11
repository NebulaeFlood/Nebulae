using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL001AnalyzerTests
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

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL001", "IL.Ref(typeof(string))"));
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

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL001", "IL.Ref(typeof(string))"));
    }

    [TestMethod]
    public async Task DirectReferenceChainsProduceNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Target
            {
                public int Value { get; set; }
            }

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Callvirt(IL.Ref(typeof(Target)).Property(nameof(Target.Value)).Get);
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task MemberReferencesProduceNoDiagnosticsWhenConsumedDirectly()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Target
            {
                public int Value;
            }

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Ldfld(IL.Ref(typeof(Target)).Field(nameof(Target.Value)));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task GenericReferencesProduceNoDiagnosticsWhenConsumedDirectly()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class GenericTarget
            {
                public static T Identity<T>(T value) => value;
            }

            static class Example
            {
                public static void Use()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(GenericTarget))
                            .Method(nameof(GenericTarget.Identity), typeof(GenericRef), typeof(GenericRef))
                            .MakeGeneric(typeof(int)));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }
}
