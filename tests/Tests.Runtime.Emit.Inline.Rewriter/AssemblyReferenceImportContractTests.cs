using Mono.Cecil;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class AssemblyReferenceImportContractTests
{
    [TestMethod]
    public void MultipleMethods_ImportSameTransitiveReference_CommitsOneCanonicalIdentity()
    {
        using var directory = new TemporaryDirectory("canonical-import");
        CompilationArtifact contracts = CompileContracts(
            directory.GetPath("contracts"),
            new Version(1, 0, 0, 0));
        CompilationArtifact dependency = CompileDependency(
            directory.GetPath("dependency"),
            "DependencyA",
            "DependencyA",
            contracts.AssemblyPath);
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static void First()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(DependencyA.Factory))
                            .Method("Create"));
                    IL.Emit.Pop();
                    IL.Emit.Ret();
                }

                public static void Second()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(DependencyA.Factory))
                            .Method("Create"));
                    IL.Emit.Pop();
                    IL.Emit.Ret();
                }
            }
            """;
        CompilationArtifact consumer = CompilationHarness.Compile(
            source,
            "CanonicalImportScenario",
            directory.GetPath("consumer"),
            additionalReferencePaths: [dependency.AssemblyPath]);
        string[] rewriteReferences =
        [
            .. consumer.ReferencePaths,
            contracts.AssemblyPath,
        ];

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            consumer.AssemblyPath,
            rewriteReferences);

        AssertRewriteSucceeded(rewrite);
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(
            consumer.AssemblyPath);
        AssemblyNameReference[] imported = [.. assembly.MainModule.AssemblyReferences.Where(
            static reference => reference.Name == "SharedContracts")];

        Assert.HasCount(1, imported);
        Assert.AreEqual(new Version(1, 0, 0, 0), imported[0].Version);
        Assert.IsFalse(AssemblyInspector.ReferencesAssembly(
            consumer.AssemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
    }

    [TestMethod]
    public void MultipleMethods_ImportConflictingTransitiveIdentities_RejectAndPreserveInput()
    {
        using var directory = new TemporaryDirectory("conflicting-imports");
        CompilationArtifact contractsV1 = CompileContracts(
            directory.GetPath("contracts-v1"),
            new Version(1, 0, 0, 0));
        CompilationArtifact contractsV2 = CompileContracts(
            directory.GetPath("contracts-v2"),
            new Version(2, 0, 0, 0));
        CompilationArtifact dependencyA = CompileDependency(
            directory.GetPath("dependency-a"),
            "DependencyA",
            "DependencyA",
            contractsV1.AssemblyPath);
        CompilationArtifact dependencyB = CompileDependency(
            directory.GetPath("dependency-b"),
            "DependencyB",
            "DependencyB",
            contractsV2.AssemblyPath);
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static void First()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(DependencyA.Factory))
                            .Method("Create"));
                    IL.Emit.Pop();
                    IL.Emit.Ret();
                }

                public static void Second()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(DependencyB.Factory))
                            .Method("Create"));
                    IL.Emit.Pop();
                    IL.Emit.Ret();
                }
            }
            """;
        CompilationArtifact consumer = CompilationHarness.Compile(
            source,
            "ConflictingImportScenario",
            directory.GetPath("consumer"),
            additionalReferencePaths:
            [
                dependencyA.AssemblyPath,
                dependencyB.AssemblyPath,
            ]);
        string hash = AssemblyInspector.ComputeSha256(consumer.AssemblyPath);
        string[] rewriteReferences =
        [
            .. consumer.ReferencePaths,
            contractsV1.AssemblyPath,
            contractsV2.AssemblyPath,
        ];

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            consumer.AssemblyPath,
            rewriteReferences);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains(
            "Imported members require incompatible identities",
            rewrite.Errors[0].Message ?? string.Empty);
        Assert.Contains("SharedContracts, Version=1.0.0.0", rewrite.Errors[0].Message ?? string.Empty);
        Assert.Contains("SharedContracts, Version=2.0.0.0", rewrite.Errors[0].Message ?? string.Empty);
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(consumer.AssemblyPath));
    }

    private static CompilationArtifact CompileContracts(
        string outputDirectory,
        Version version)
    {
        string source = $$"""
            [assembly: System.Reflection.AssemblyVersion("{{version}}")]

            namespace SharedContracts;

            public sealed class Payload
            {
            }
            """;

        return CompilationHarness.Compile(
            source,
            "SharedContracts",
            outputDirectory);
    }

    private static CompilationArtifact CompileDependency(
        string outputDirectory,
        string assemblyName,
        string @namespace,
        string contractsPath)
    {
        string source = $$"""
            namespace {{@namespace}};

            public static class Factory
            {
                public static SharedContracts.Payload Create() => new();
            }
            """;

        return CompilationHarness.Compile(
            source,
            assemblyName,
            outputDirectory,
            additionalReferencePaths: [contractsPath]);
    }

    private static void AssertRewriteSucceeded(InlineILTaskResult rewrite)
    {
        Assert.IsTrue(
            rewrite.Success,
            string.Join(Environment.NewLine, rewrite.Errors.Select(static error => error.Message)));
        Assert.IsEmpty(rewrite.Errors);
        Assert.IsEmpty(rewrite.Warnings);
    }
}
