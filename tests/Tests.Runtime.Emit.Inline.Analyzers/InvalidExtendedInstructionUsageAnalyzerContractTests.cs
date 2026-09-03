using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class InvalidExtendedInstructionUsageAnalyzerContractTests
{
    [TestMethod]
    public async Task Fail_WhenStoredInsteadOfThrown_ReportsNEBIL2002_OnInvocation()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Exception exception = IL.Fail();
                    _ = exception;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL2002",
            "IL.Fail()");
    }

    [TestMethod]
    public async Task Fail_WhenNestedInsideThrownExpression_ReportsNEBIL2002_OnInvocation()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    throw Wrap(IL.Fail());
                }

                static Exception Wrap(Exception exception) => exception;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2002", "IL.Fail()");
    }

    [TestMethod]
    public async Task Fail_WhenDirectlyThrownWithOrWithoutConversion_HasNoDiagnostics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Direct()
                {
                    throw IL.Fail();
                }

                static void Converted()
                {
                    throw (Exception)IL.Fail();
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task RetOfT_WhenStoredInsteadOfReturned_ReportsNEBIL2002_OnInvocation()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static int Run()
                {
                    int result = IL.Ret<int>();
                    return result;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2002", "IL.Ret<int>()");
    }

    [TestMethod]
    public async Task RetOfT_WhenNestedInsideReturnedExpression_ReportsNEBIL2002_OnInvocation()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static int Run()
                {
                    return Identity(IL.Ret<int>());
                }

                static T Identity<T>(T value) => value;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL2002", "IL.Ret<int>()");
    }

    [TestMethod]
    public async Task RetOfT_WhenDirectlyReturnedWithOrWithoutConversion_HasNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static int Direct()
                {
                    return IL.Ret<int>();
                }

                static object Converted()
                {
                    return (object)IL.Ret<string>();
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task PrimitiveRet_WhenUsedAsInstruction_HasNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Ret();
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }
}
