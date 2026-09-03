using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class InvalidInstructionUsageAnalyzerContractTests
{
    [TestMethod]
    public async Task Instruction_WhenUsedAsMethodGroup_ReportsNEBIL2001_OnMethodReference()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Action action = IL.Emit.Nop;
                    action();
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL2001",
            "IL.Emit.Nop");
    }

    [TestMethod]
    public async Task ExtendedInstruction_WhenUsedAsMethodGroup_ReportsNEBIL2001_OnMethodReference()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Func<Exception> fail = IL.Fail;
                    _ = fail;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2001", "IL.Fail");
    }

    [TestMethod]
    public async Task ExtendedInstruction_WhenUsedInFieldInitializer_ReportsNEBIL2001_OnInvocation()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                private static readonly object Value = IL.Ret<object>();
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2001", "IL.Ret<object>()");
    }

    [TestMethod]
    public async Task Instruction_WhenUsedInExpressionTree_ReportsNEBIL2001_OnInvocation()
    {
        const string source = """
            using System;
            using System.Linq.Expressions;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Expression<Action> expression = () => IL.Emit.Nop();
                    _ = expression;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2001", "IL.Emit.Nop()");
    }

    [TestMethod]
    public async Task Instruction_WhenCalledInExecutableLambda_HasNoDiagnostics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Action action = () => IL.Emit.Nop();
                    action();
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }
}
