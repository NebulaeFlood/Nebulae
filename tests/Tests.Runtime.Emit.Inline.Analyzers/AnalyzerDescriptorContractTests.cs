using Microsoft.CodeAnalysis;
using Nebulae.Runtime.Emit.Inline.Analyzers;

namespace Tests.Runtime.Emit.Inline.Analyzers;

[TestClass]
public sealed class AnalyzerDescriptorContractTests
{
    [TestMethod]
    public void SupportedDiagnostics_AreOrderedByRuleClassAndSequence()
    {
        string[] expectedIds =
        [
            "NEBIL1001",
            "NEBIL1002",
            "NEBIL2001",
            "NEBIL2002",
            "NEBIL3001",
            "NEBIL3002",
            "NEBIL4001",
            "NEBIL4002",
            "NEBIL4003",
            "NEBIL5001",
            "NEBIL5002",
        ];

        string[] actualIds = [.. new PlaceholderAnalyzer()
            .SupportedDiagnostics
            .Select(static descriptor => descriptor.Id)];

        CollectionAssert.AreEqual(expectedIds, actualIds);
    }
}
