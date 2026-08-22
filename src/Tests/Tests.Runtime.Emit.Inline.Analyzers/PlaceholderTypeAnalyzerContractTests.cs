using static Tests.Runtime.Emit.Inline.Analyzers.AnalyzerDiagnosticAssert;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class PlaceholderTypeAnalyzerContractTests
{
    [TestMethod]
    public async Task PlaceholderType_WhenExposedByField_ReportsNEBIL1001_OnFieldName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                private static TypeRef? Placeholder;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "Placeholder");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenExposedByProperty_ReportsNEBIL1001_OnPropertyAndGetter()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Scenario
            {
                private TypeRef Placeholder => null!;
            }
            """;

        await AssertDiagnosticsAsync(source, "NEBIL1001", "Placeholder", "null!");
    }

    [TestMethod]
    public async Task PlaceholderType_InIndexerParameter_ReportsNEBIL1001_OnIndexerAndGetter()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Scenario
            {
                public object this[TypeRef value] => null!;
            }
            """;

        await AssertDiagnosticsAsync(source, "NEBIL1001", "this", "null!");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenExposedByEvent_ReportsNEBIL1001_OnEventName()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            sealed class Scenario
            {
                private event Action<TypeRef>? Placeholder;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "Placeholder");
    }

    [TestMethod]
    public async Task PlaceholderType_WhenStoredInLocal_ReportsNEBIL1001_OnDeclarator()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    TypeRef? placeholder = null;
                    _ = placeholder;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "placeholder = null");
    }

    [TestMethod]
    public async Task PlaceholderType_InMethodReturnAndParameter_ReportsNEBIL1001_OnMethodNames()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                private static TypeRef Produce() => null!;

                private static void Consume(TypeRef value) { }
            }
            """;

        await AssertDiagnosticsAsync(source, "NEBIL1001", "Produce", "Consume");
    }

    [TestMethod]
    public async Task PlaceholderType_InAttributeConstructorParameter_ReportsNEBIL1001_OnConstructorName()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            sealed class ScenarioAttribute : Attribute
            {
                public ScenarioAttribute(TypeRef value) { }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "ScenarioAttribute");
    }

    [TestMethod]
    public async Task PlaceholderType_InDelegateSignature_ReportsNEBIL1001_OnDelegateName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Container
            {
                private delegate TypeRef Scenario(TypeRef value);
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "Scenario");
    }

    [TestMethod]
    public async Task PlaceholderType_InGenericArgumentAndArrayElement_ReportsNEBIL1001_OnFieldNames()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                private static Dictionary<string, TypeRef>? GenericPlaceholder;
                private static TypeRef[]? ArrayPlaceholder;
            }
            """;

        await AssertDiagnosticsAsync(
            source,
            "NEBIL1001",
            "GenericPlaceholder",
            "ArrayPlaceholder");
    }

    [TestMethod]
    public async Task PlaceholderType_InMethodAndTypeConstraints_ReportsNEBIL1001_OnDeclarations()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class MethodScenario
            {
                public static void Run<T>() where T : IEnumerable<TypeRef> { }
            }

            sealed class TypeScenario<T> where T : IEnumerable<TypeRef> { }
            """;

        await AssertDiagnosticsAsync(source, "NEBIL1001", "Run", "TypeScenario");
    }

    [TestMethod]
    public async Task PlaceholderType_InBaseTypeAndInterface_ReportsNEBIL1001_OnTypeNames()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            class Base<T> { }
            interface IContract<T> { }

            sealed class BaseScenario : Base<TypeRef> { }
            sealed class InterfaceScenario : IContract<TypeRef> { }
            """;

        await AssertDiagnosticsAsync(
            source,
            "NEBIL1001",
            "BaseScenario",
            "InterfaceScenario");
    }

    [TestMethod]
    public async Task PlaceholderType_InContainingGenericType_ReportsNEBIL1001_OnFieldName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            sealed class Outer<T>
            {
                public sealed class Inner { }
            }

            static class Scenario
            {
                private static Outer<TypeRef>.Inner? Placeholder;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "Placeholder");
    }

    [TestMethod]
    public async Task PlaceholderType_InFunctionPointerSignature_ReportsNEBIL1001_OnFieldName()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            unsafe static class Scenario
            {
                private static delegate*<TypeRef, TypeRef> Placeholder;
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "Placeholder");
    }

    [TestMethod]
    public async Task ReferenceApi_WhenUsedAsMethodGroup_ReportsNEBIL1001_OnDelegateVariable()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            static class Scenario
            {
                static void Run()
                {
                    Func<Type, TypeRef> reference = IL.Ref;
                    _ = reference;
                }
            }
            """;

        await AssertSingleDiagnosticAsync(source, "NEBIL1001", "reference = IL.Ref");
    }
}
