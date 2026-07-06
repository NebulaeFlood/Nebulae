using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Runtime.Emit.Inline.Support;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class InvalidPlaceholderUsageAnalyzerTests
{
    [TestMethod]
    public async Task ReferenceTypesCannotAppearInMethodSignatures()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static TypeRef Expose(TypeRef value) => value;
            }
            """;

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL002", diagnostics[0].Id);
        Assert.AreEqual("Expose", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }

    [TestMethod]
    public async Task ReferenceTypesCannotBeStoredInLocalVariables()
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

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL002", diagnostics[0].Id);
        Assert.AreEqual("reference = null!", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }

    [TestMethod]
    public async Task ReferenceTypesCannotBeNestedInMemberTypes()
    {
        const string source = """
            using System.Collections.Generic;
            using Nebulae.Runtime.Emit.Inline;

            static class Example
            {
                public static List<TypeRef> Expose(List<TypeRef> value) => value;
            }
            """;

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL002", diagnostics[0].Id);
        Assert.AreEqual("Expose", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }

    [TestMethod]
    public async Task ReferenceTypesCannotAppearInInheritedInterfaces()
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

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL002", diagnostics[0].Id);
        Assert.AreEqual("InvalidContainer", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }

    [TestMethod]
    public async Task ReferenceTypesCannotAppearInGenericConstraints()
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

        var diagnostics = await AnalyzerTestHost.GetDiagnosticsAsync(source);

        Assert.HasCount(1, diagnostics);
        Assert.AreEqual("NEBIL002", diagnostics[0].Id);
        Assert.AreEqual("Constrain", AnalyzerTestHost.GetSourceSnippet(source, diagnostics[0]));
    }
}
