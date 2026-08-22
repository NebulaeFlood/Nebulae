using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class ConstantOperandAnalyzerContractTests
{
    [TestMethod]
    public async Task ScalarOperand_WhenNotCompileTimeConstant_ReportsNEBIL4001_OnOperand()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(string value)
                {
                    IL.Emit.Ldstr(value);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4001", "value");
    }

    [TestMethod]
    public async Task BranchArrayItem_WhenNotCompileTimeConstant_ReportsNEBIL4001_OnItem()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(string target)
                {
                    IL.Emit.Switch("defined", target);
                    IL.Label("defined");
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4001", "target");
    }

    [TestMethod]
    public async Task TypeOperand_WhenNotTypeOfExpression_ReportsNEBIL4001_OnOperand()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(Type type)
                {
                    IL.Emit.Box(type);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4001", "type");
    }

    [TestMethod]
    public async Task TypeArrayItem_WhenNotTypeOfExpression_ReportsNEBIL4001_OnItem()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(Type parameterType)
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(string))
                            .Method(nameof(string.Concat), parameterType));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4001", "parameterType");
    }

    [TestMethod]
    public async Task GenericParameterCount_WhenNotCompileTimeConstant_ReportsNEBIL4001_OnCount()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run(int count)
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Generic", count));
                }

                static void Generic<T>() { }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4001", "count");
    }

    [TestMethod]
    public async Task Unaligned_WhenConstantIsUnsupported_ReportsNEBIL4002_OnValue()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Unaligned(3);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4002", "3");
    }

    [TestMethod]
    public async Task No_WhenConstantsAreOutsideSupportedFlags_ReportsNEBIL4002_OnValues()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.No(0);
                    IL.Emit.No(8);
                }
            }
            """;

        await AssertDiagnosticsAsync(source, "NEBIL4002", "0", "8");
    }

    [TestMethod]
    public async Task Label_WhenNameIsEmpty_ReportsNEBIL4002_OnName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Label("");
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4002", "\"\"");
    }

    [TestMethod]
    public async Task ReferenceMemberName_WhenEmpty_ReportsNEBIL4002_OnName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(IL.Ref(typeof(Scenario)).Method(""));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4002", "\"\"");
    }

    [TestMethod]
    public async Task GenericParameterCount_WhenNotPositive_ReportsNEBIL4002_OnCount()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(IL.Ref(typeof(Scenario)).Method("Generic", 0));
                }

                static void Generic<T>() { }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL4002", "0");
    }

    [TestMethod]
    public async Task ConstantOperands_AtSupportedBoundaries_HaveNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Unaligned(1);
                    IL.Emit.Unaligned(2);
                    IL.Emit.Unaligned(4);
                    IL.Emit.No(1);
                    IL.Emit.No(7);
                    IL.Emit.Ldstr("");
                    IL.Emit.Br("defined");
                    IL.Label("defined");
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Generic", 1)
                            .MakeGeneric(typeof(int)));
                }

                static void Generic<T>() { }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }
}
