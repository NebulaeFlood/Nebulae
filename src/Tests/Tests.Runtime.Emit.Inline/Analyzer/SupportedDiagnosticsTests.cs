using Microsoft.CodeAnalysis;
using Nebulae.Runtime.Emit.Inline.Analyzers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class SupportedDiagnosticsTests
{
    private static readonly string[] ExpectedDiagnosticIds =
    [
        "NEBIL001",
        "NEBIL002",
        "NEBIL003",
        "NEBIL004",
        "NEBIL005",
        "NEBIL006",
        "NEBIL007",
    ];

    [TestMethod]
    public void SupportedDiagnostics_ExposeEveryRuleOnceWithExpectedMetadata()
    {
        DiagnosticDescriptor[] descriptors = [.. new PlaceholderAnalyzer().SupportedDiagnostics];

        CollectionAssert.AreEqual(
            ExpectedDiagnosticIds,
            descriptors.Select(static descriptor => descriptor.Id).ToArray());

        Assert.IsTrue(descriptors.All(static descriptor => descriptor.Category == "Usage"));
        Assert.IsTrue(descriptors.All(static descriptor => descriptor.DefaultSeverity == DiagnosticSeverity.Error));
        Assert.IsTrue(descriptors.All(static descriptor => descriptor.IsEnabledByDefault));
    }
}
