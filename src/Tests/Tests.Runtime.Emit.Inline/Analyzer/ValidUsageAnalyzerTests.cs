using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class ValidUsageAnalyzerTests
{
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

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);

        Assert.IsEmpty(diagnostics);
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

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);

        Assert.IsEmpty(diagnostics);
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

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);

        Assert.IsEmpty(diagnostics);
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

        var diagnostics = await AnalyzerTestHelpers.GetDiagnosticsAsync(source);

        Assert.IsEmpty(diagnostics);
    }
}
