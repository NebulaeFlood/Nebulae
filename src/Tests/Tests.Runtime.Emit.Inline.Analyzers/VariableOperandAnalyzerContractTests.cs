using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class VariableOperandAnalyzerContractTests
{
    [TestMethod]
    public async Task ArgumentOperand_WhenItIsNotCurrentFunctionParameter_ReportsNEBIL4003_OnOperand()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Ldarg(42);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4003", "42");
    }

    [TestMethod]
    public async Task ArgumentOperand_WhenCapturedFromOuterFunction_ReportsNEBIL4003_InLocalFunction()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(int outer)
                {
                    Local();

                    void Local()
                    {
                        IL.Emit.Ldarg(outer);
                    }
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4003", "outer");
    }

    [TestMethod]
    public async Task ValueOperand_AcrossValueReferenceAndPointerForms_HasNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static unsafe void Run(int outer)
                {
                    int local = 0;
                    int* pointer = &local;

                    IL.Push(local);
                    IL.Push(ref local);
                    IL.Push(pointer);
                    IL.Push((void*)pointer);

                    Local();

                    void Local()
                    {
                        IL.Push(outer);
                    }
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }
}
