using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL004AnalyzerTests
{
    [TestMethod]
    public async Task CompileTimeConstantExpressionsProduceNoDiagnostics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                private const int BaseValue = 20;

                public static void Use()
                {
                    IL.Emit.Ldc_I4(BaseValue + 60 + (80 + 60));
                    IL.Emit.Ldc_I8(20L + 60L);
                    IL.Emit.Ldc_R4(20F + 60F);
                    IL.Emit.Ldc_R8(20D + 60D);
                    IL.Emit.Ldstr(nameof(Example));
                    IL.Emit.Br("target");
                    IL.Label("target");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task RuntimeValueProducesNonConstantOperandDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use(int value)
                {
                    IL.Emit.Ldc_I4(value);
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "value", 1));
    }

    [TestMethod]
    public async Task SwitchRequiresConstantLabels()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use(string label, string[] labels)
                {
                    IL.Emit.Switch("first", nameof(Example));
                    IL.Emit.Switch(new[] { "first", "second" });
                    IL.Emit.Switch(Array.Empty<string>());
                    IL.Emit.Switch(label, "second");
                    IL.Emit.Switch(labels);
                    IL.Label("first");
                    IL.Label(nameof(Example));
                    IL.Label("second");
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "label", 2),
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "labels", 1));
    }

    [TestMethod]
    public async Task DirectTypeOfOperandsProduceNoDiagnostics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Target<T>(int value) { }

                public static void Use()
                {
                    IL.Emit.Newarr(typeof(int));
                    IL.Emit.Call(
                        IL.Ref(typeof(Example))
                            .Method(nameof(Target), typeof(void), typeof(GenericRef), typeof(int))
                            .MakeGeneric(typeof(string)));
                    IL.Emit.Newobj(IL.Ref(typeof(Example)).Constructor(Array.Empty<Type>()));
                    IL.Emit.Newobj(IL.Ref(typeof(object)).Constructor(Type.EmptyTypes));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task RuntimeTypeOperationsProduceNonConstantOperandDiagnostics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use(Type type)
                {
                    IL.Emit.Newarr(type);
                    IL.Emit.Box(typeof(int).MakeByRefType());
                    IL.Emit.Call(IL.Ref(typeof(Tuple<,>).MakeGenericType(typeof(int), typeof(string))).Method("Target"));
                    IL.Emit.Call(IL.Ref(Type.GetType("System.String")!).Method("Target"));
                    IL.Emit.Call(IL.Ref(typeof(string).GetMember(nameof(string.Length))[0].DeclaringType!).Method("Target"));
                    IL.Emit.Call(IL.Ref(typeof(Example)).Method("Use", new[] { typeof(int), type }));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL004"),
            AnalyzerTestHelpers.Diagnostic("NEBIL004"),
            AnalyzerTestHelpers.Diagnostic("NEBIL004"),
            AnalyzerTestHelpers.Diagnostic("NEBIL004"),
            AnalyzerTestHelpers.Diagnostic("NEBIL004"),
            AnalyzerTestHelpers.Diagnostic("NEBIL004"));
    }

    [TestMethod]
    public async Task RuntimeMemberNamesProduceNonConstantOperandDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Target
            {
                public int Field;
                public int Property { get; set; }
                public event System.Action? Event;
                public void Method() { }
            }

            static class Example
            {
                public static void Use(string name)
                {
                    IL.Emit.Ldfld(IL.Ref(typeof(Target)).Field(name));
                    IL.Emit.Call(IL.Ref(typeof(Target)).Property(name).Get);
                    IL.Emit.Call(IL.Ref(typeof(Target)).Event(name).Add);
                    IL.Emit.Call(IL.Ref(typeof(Target)).Method(name));
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "name", 1),
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "name", 2),
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "name", 3),
            AnalyzerTestHelpers.Diagnostic("NEBIL004", "name", 4));
    }
}
