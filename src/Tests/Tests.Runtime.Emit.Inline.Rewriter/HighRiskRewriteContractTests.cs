using Microsoft.CodeAnalysis;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class HighRiskRewriteContractTests
{
    [TestMethod]
    public async Task StorageAndMemberReferences_AfterRewrite_PreserveObservableSemantics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public sealed class Target
            {
                public int Field;

                public int Value { get; }

                public Target(int value)
                {
                    Field = value;
                    Value = value;
                }

                public int this[int offset] => Value + offset;
            }

            public static class Scenario
            {
                private static Target Create(int value)
                {
                    IL.Emit.Ldarg(value);
                    IL.Emit.Newobj(
                        IL.Ref(typeof(Target))
                            .Constructor(typeof(int)));
                    return IL.Ret<Target>();
                }

                private static int ReadMembers(Target target)
                {
                    IL.Emit.Ldarg(target);
                    IL.Emit.Ldfld(
                        IL.Ref(typeof(Target))
                            .Field(nameof(Target.Field)));
                    IL.Emit.Ldarg(target);
                    IL.Emit.Callvirt(
                        IL.Ref(typeof(Target))
                            .Property(nameof(Target.Value))
                            .Get);
                    IL.Emit.Add();
                    IL.Emit.Ldarg(target);
                    IL.Emit.Ldc_I4(2);
                    IL.Emit.Callvirt(
                        IL.Ref(typeof(Target))
                            .Indexer(typeof(int))
                            .Get);
                    IL.Emit.Add();
                    return IL.Ret<int>();
                }

                private static void Increment(ref int value)
                {
                    IL.Emit.Ldarg(value);
                    IL.Emit.Dup();
                    IL.Emit.Ldind_I4();
                    IL.Emit.Ldc_I4(1);
                    IL.Emit.Add();
                    IL.Emit.Stind_I4();
                    IL.Emit.Ret();
                }

                private static int LocalRoundTrip()
                {
                    IL.Emit.Ldc_I4(41);
                    IL.Emit.Stloc(out int local);
                    IL.Emit.Ldloc(local);
                    IL.Emit.Ldc_I4(1);
                    IL.Emit.Add();
                    return IL.Ret<int>();
                }

                public static int Main()
                {
                    Target target = Create(10);
                    int value = 41;
                    Increment(ref value);
                    Console.Write($"{ReadMembers(target)},{value},{LocalRoundTrip()}");
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("storage-members");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "StorageMemberScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        AssertRewriteSucceeded(InlineILTaskHarness.Execute(artifact));
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("32,42,42", execution.StandardOutput);
    }

    [TestMethod]
    public async Task ManagedFunctionPointer_AfterRewrite_PreservesIndirectCallSemantics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                private static int Add(int left, int right) => left + right;

                private static int InvokeIndirectly()
                {
                    IL.Emit.Ldc_I4(19);
                    IL.Emit.Ldc_I4(23);
                    IL.Emit.Ldftn(
                        IL.Ref(typeof(Scenario))
                            .Method(nameof(Add), typeof(int), typeof(int)));
                    IL.Emit.Calli(
                        IL.Ref(typeof(Scenario))
                            .Method(nameof(Add), typeof(int), typeof(int)));
                    return IL.Ret<int>();
                }

                public static int Main()
                {
                    Console.Write(InvokeIndirectly());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("function-pointer");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "FunctionPointerScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        AssertRewriteSucceeded(InlineILTaskHarness.Execute(artifact));
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    [TestMethod]
    public async Task Rethrow_AfterRewrite_PreservesOriginalException()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                private static void ThrowAndRethrow()
                {
                    try
                    {
                        throw new InvalidOperationException("boom");
                    }
                    catch
                    {
                        IL.Emit.Rethrow();
                    }
                }

                public static int Main()
                {
                    try
                    {
                        ThrowAndRethrow();
                        return 1;
                    }
                    catch (InvalidOperationException error)
                    {
                        Console.Write(error.Message);
                        return 0;
                    }
                }
            }
            """;
        using var directory = new TemporaryDirectory("rethrow");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "RethrowScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        AssertRewriteSucceeded(InlineILTaskHarness.Execute(artifact));
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("boom", execution.StandardOutput);
    }

    [TestMethod]
    public async Task Prefixes_AfterRewrite_AreAdjacentToConsumersAndRunCorrectly()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                private static int Value = 42;

                private static int ReadVolatile()
                {
                    IL.Emit.Volatile();
                    IL.Emit.Ldsfld(
                        IL.Ref(typeof(Scenario))
                            .Field(nameof(Value)));
                    return IL.Ret<int>();
                }

                private static string Format<T>(T value)
                    where T : struct
                {
                    IL.Emit.Ldarga(value);
                    IL.Emit.Constrained(typeof(T));
                    IL.Emit.Callvirt(
                        IL.Ref(typeof(object))
                            .Method(nameof(ToString)));
                    return IL.Ret<string>();
                }

                public static int Main()
                {
                    Console.Write($"{ReadVolatile()},{Format(7)}");
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("prefixes");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "PrefixScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        AssertRewriteSucceeded(InlineILTaskHarness.Execute(artifact));
        string[] volatileInstructions = [.. AssemblyInspector.GetMethodInstructions(
            artifact.AssemblyPath,
            "Scenario",
            "ReadVolatile")];
        string[] constrainedInstructions = [.. AssemblyInspector.GetMethodInstructions(
            artifact.AssemblyPath,
            "Scenario",
            "Format")];
        int volatileIndex = Array.IndexOf(volatileInstructions, "volatile.");
        int constrainedIndex = Array.IndexOf(constrainedInstructions, "constrained. type:T");

        Assert.IsGreaterThanOrEqualTo(0, volatileIndex);
        Assert.StartsWith("ldsfld field:", volatileInstructions[volatileIndex + 1]);
        Assert.IsGreaterThanOrEqualTo(0, constrainedIndex);
        Assert.StartsWith("callvirt method:", constrainedInstructions[constrainedIndex + 1]);

        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42,7", execution.StandardOutput);
    }

    private static void AssertRewriteSucceeded(InlineILTaskResult rewrite)
    {
        Assert.IsTrue(
            rewrite.Success,
            string.Join(Environment.NewLine, rewrite.Errors.Select(static error => error.Message)));
        Assert.IsEmpty(rewrite.Errors);
        Assert.IsEmpty(rewrite.Warnings);
    }

    private static Task<ProcessResult> RunRewrittenProgramAsync(CompilationArtifact artifact)
    {
        string assemblyName = typeof(HighRiskRewriteContractTests).Assembly.GetName().Name!;
        string runtimeConfig = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            $"{assemblyName}.runtimeconfig.json");

        return ProcessRunner.RunAsync(
            "dotnet",
            ["exec", "--runtimeconfig", runtimeConfig, artifact.AssemblyPath],
            System.IO.Path.GetDirectoryName(artifact.AssemblyPath)!,
            TimeSpan.FromSeconds(30));
    }
}
