using Tests.Runtime.Emit.Inline.Helpers;

namespace Tests.Runtime.Emit.Inline;

[TestClass]
[DoNotParallelize]
public sealed class BuildScenarioTests
{
    [TestMethod]
    [DataRow("UndefinedLabel", "Label 'missing' is not defined.")]
    [DataRow("DuplicateLabel", "Duplicate label 'duplicate' defined.")]
    [DataRow("InvalidFail", "Cannot resolve IL.Fail, the instruction sequence is incompatible.")]
    [DataRow("InvalidTail", "Invalid IL code, the 'tail.' prefix must be followed by a call/calli/callvirt instruction.")]
    [DataRow("MissingMethod", "Cannot find any method named 'Missing' in type 'System.String'.")]
    [DataRow("EscapingReference", "Cannot remove the reference to 'Nebulae.Runtime.Emit.Inline'.")]
    public async Task InvalidInlineIL_FailsBuildWithExpectedMessage(string scenarioName, string expectedMessage)
    {
        BuildScenarioResult result = await BuildScenarioTestHelpers.BuildFailureScenarioAsync(scenarioName);

        AssertBuildFailedForExpectedReason(result, expectedMessage);
    }

    [TestMethod]
    public async Task PortableSymbols_ReportSourceLocation()
    {
        BuildScenarioResult result = await BuildScenarioTestHelpers.BuildFailureScenarioAsync("UndefinedLabel", "portable");

        AssertBuildFailedForExpectedReason(result, "Label 'missing' is not defined.");
        Assert.MatchesRegex(@"UndefinedLabel\.cs\(\d+,\d+", result.Output);
    }

    [TestMethod]
    public async Task BuildWithoutSymbols_StillReportsSemanticError()
    {
        BuildScenarioResult result = await BuildScenarioTestHelpers.BuildFailureScenarioAsync("UndefinedLabel", "none");

        AssertBuildFailedForExpectedReason(result, "Label 'missing' is not defined.");
    }

    [TestMethod]
    public async Task PackageReference_WithoutPrivateAssetsAll_FailsValidation()
    {
        BuildScenarioResult result = await BuildScenarioTestHelpers.ValidatePrivateAssetsAsync();

        AssertBuildFailedForExpectedReason(
            result,
            "The package reference for Nebulae.Runtime.Emit.Inline must contain PrivateAssets='all'.");
    }

    private static void AssertBuildFailedForExpectedReason(BuildScenarioResult result, string expectedMessage)
    {
        Assert.AreNotEqual(0, result.ExitCode, $"The invalid build scenario unexpectedly succeeded:{Environment.NewLine}{result.Output}");
        Assert.Contains(expectedMessage, result.Output);
        Assert.IsFalse(result.Output.Contains("NU1301", StringComparison.Ordinal), $"The scenario failed during restore:{Environment.NewLine}{result.Output}");
        Assert.IsFalse(result.Output.Contains("MSB4062", StringComparison.Ordinal), $"The Inline IL task could not be loaded:{Environment.NewLine}{result.Output}");
    }
}
