using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class NEBIL002AnalyzerTests
{
    [TestMethod]
    public async Task ReferenceType_InMethodSignature_ReportsDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static TypeRef Expose(TypeRef value) => value;
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL002", "Expose"));
    }

    [TestMethod]
    public async Task ReferenceType_InLocalVariable_ReportsDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Use()
                {
                    TypeRef reference = null!;
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL002", "reference = null!"));
    }

    [TestMethod]
    public async Task ReferenceType_NestedInMemberType_ReportsDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static List<TypeRef> Expose(List<TypeRef> value) => value;
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL002", "Expose"));
    }

    [TestMethod]
    public async Task ReferenceType_InInheritedInterface_ReportsDiagnostic()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            interface IContainer<T>
            {
            }

            sealed class InvalidContainer : IContainer<TypeRef>
            {
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL002", "InvalidContainer"));
    }

    [TestMethod]
    public async Task ReferenceType_InGenericConstraint_ReportsDiagnostic()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static void Constrain<T>() where T : IEnumerable<TypeRef>
                {
                }
            }
            """;

        await AnalyzerTestHelpers.VerifyDiagnosticsAsync(
            source,
            AnalyzerTestHelpers.Diagnostic("NEBIL002", "Constrain"));
    }
}
