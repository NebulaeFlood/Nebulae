using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class SupportedUsageAnalyzerContractTests
{
    [TestMethod]
    public async Task SupportedDirectUsage_AcrossReferenceConstantLabelAndVariableBoundaries_HasNoDiagnostics()
    {
        const string source = """
            using System;
            using System.Linq;
            using Nebulae.Runtime.Emit.Inline;

            sealed class Scenario
            {
                void Run(int argument)
                {
                    int local = 0;

                    IL.Emit.Ldarg(this);
                    IL.Emit.Ldarg(argument);
                    IL.Emit.Ldloc(local);
                    IL.Emit.Stloc(out int result);
                    IL.Emit.Unaligned(1);
                    IL.Emit.No(7);
                    IL.Emit.Call(
                        IL.Ref(typeof(string))
                            .Method(nameof(string.Concat), typeof(string), typeof(string)));
                    IL.Emit.Call(
                        IL.Ref(typeof(Enumerable))
                            .Method(
                                nameof(Enumerable.Repeat),
                                1,
                                typeof(GenericRef),
                                typeof(int))
                            .MakeGeneric(typeof(string)));
                    IL.Emit.Br("done");
                    IL.Label("done");

                    _ = result;
                }

                void Other()
                {
                    IL.Label("same-name-in-another-function");

                    void Local()
                    {
                        IL.Label("same-name-in-another-function");
                    }

                    Local();
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }
}
