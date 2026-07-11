using Microsoft.CodeAnalysis;
using Nebulae.Runtime.Emit.Inline.Analyzers;

namespace Tests.Runtime.Emit.Inline.Analyzer;

[TestClass]
public sealed class SupportedDiagnosticsTests
{
    [TestMethod]
    public void EveryRuleIsExposedOnceWithTheExpectedMetadata()
    {
        DiagnosticDescriptor[] descriptors = [.. new PlaceholderAnalyzer().SupportedDiagnostics];

        CollectionAssert.AreEqual(
            new[]
            {
                "NEBIL001",
                "NEBIL002",
                "NEBIL003",
                "NEBIL004",
                "NEBIL005",
                "NEBIL006",
                "NEBIL007"
            },
            descriptors.Select(static descriptor => descriptor.Id).ToArray());

        Assert.IsTrue(descriptors.All(static descriptor => descriptor.Category == "Usage"));
        Assert.IsTrue(descriptors.All(static descriptor => descriptor.DefaultSeverity == DiagnosticSeverity.Error));
        Assert.IsTrue(descriptors.All(static descriptor => descriptor.IsEnabledByDefault));
    }
}
