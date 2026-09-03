using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class InvalidPlaceholderUsageAnalyzerContractTests
{
    [TestMethod]
    public async Task PlaceholderType_WhenUsedByTypeReference_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(TypeRef))
                            .Method("Method"));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(TypeRef)");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenUsedByConstructorReference_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Newobj(
                        IL.Ref(typeof(Scenario))
                            .Constructor(typeof(TypeRef)));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(TypeRef)");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenUsedByIndexerReference_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Indexer(typeof(TypeRef))
                            .Get);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(TypeRef)");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenUsedByMethodReference_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Method", typeof(TypeRef)));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(TypeRef)");
    }

    [TestMethod]
    public async Task NestedPlaceholderType_WhenUsedByReferenceApi_ReportsNEBIL1002_OnEntireTypeOfExpression()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Method", typeof(List<TypeRef[]>)));
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL1002",
            "typeof(List<TypeRef[]>)");
    }

    [TestMethod]
    public async Task PlaceholderContainerTypes_WhenUsedByReferenceApi_ReportNEBIL1002_OnTypeOfExpressions()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Method", typeof(IL), typeof(IL.Emit)));
                }
            }
            """;

        await AssertDiagnosticsAsync(
            source,
            "NEBIL1002",
            "typeof(IL)",
            "typeof(IL.Emit)",
            "typeof(IL.Emit)");
    }

    [TestMethod]
    public async Task GenericRef_WhenUsedBySupportedReferenceApis_HasNoDiagnostics()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(GenericRef))
                            .Method("Method"));
                    IL.Emit.Newobj(
                        IL.Ref(typeof(Scenario))
                            .Constructor(typeof(GenericRef)));
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Indexer(typeof(GenericRef))
                            .Get);
                    IL.Emit.Call(
                        IL.Ref(typeof(Scenario))
                            .Method("Method", typeof(GenericRef)));
                }
            }
            """;

        await AssertNoDiagnosticsAsync(source);
    }

    [TestMethod]
    public async Task PlaceholderType_WhenUsedAsAttributeArgument_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            [Scenario(typeof(TypeRef))]
            sealed class Target { }

            sealed class ScenarioAttribute : Attribute
            {
                public ScenarioAttribute(Type type) { }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(TypeRef)");
    }

    [TestMethod]
    public async Task GenericRef_WhenUsedAsAttributeArgument_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            [Scenario(typeof(GenericRef))]
            sealed class Target { }

            sealed class ScenarioAttribute : Attribute
            {
                public ScenarioAttribute(Type type) { }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1002", "typeof(GenericRef)");
    }

    [TestMethod]
    public async Task GenericRef_WhenUsedByOrdinaryCode_ReportsNEBIL1002_OnTypeOfExpression()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Type type = typeof(GenericRef);
                    GC.KeepAlive(type);
                }
            }
            """;

        await AssertSingleDiagnosticAsync(
            source,
            "NEBIL1002",
            "typeof(GenericRef)");
    }
}
