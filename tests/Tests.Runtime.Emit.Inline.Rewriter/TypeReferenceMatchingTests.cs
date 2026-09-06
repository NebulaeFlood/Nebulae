using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class TypeReferenceMatchingTests
{
    [TestMethod]
    [DataRow("in")]
    [DataRow("out")]
    [DataRow("ref")]
    public void MethodReference_WithByReferenceType_MatchesParameter(
        string parameterModifier)
    {
        string methodBody = parameterModifier == "out"
            ? "value = default;"
            : string.Empty;
        string source = $$"""
            using Nebulae.Runtime.Emit.Inline;

            public static class Target
            {
                public static void Invoke({{parameterModifier}} int value)
                {
                    {{methodBody}}
                }
            }

            public static class Scenario
            {
                public static void Resolve(ref int value)
                {
                    IL.Emit.Ldarg(value);
                    IL.Emit.Call(
                        IL.Ref(typeof(Target))
                            .Method(nameof(Target.Invoke), typeof(int).MakeByRefType()));
                    IL.Emit.Ret();
                }
            }
            """;
        using var directory = new TemporaryDirectory(
            $"byref-parameter-{parameterModifier}");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            $"ByRef{parameterModifier}ParameterScenario",
            directory.DirectoryPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);

        Assert.IsTrue(
            rewrite.Success,
            string.Join(Environment.NewLine, rewrite.Errors.Select(static error => error.Message)));
        Assert.IsEmpty(rewrite.Errors);
        Assert.IsEmpty(rewrite.Warnings);
    }
}
