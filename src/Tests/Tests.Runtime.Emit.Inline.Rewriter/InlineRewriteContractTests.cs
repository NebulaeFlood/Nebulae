using Microsoft.CodeAnalysis;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class InlineRewriteContractTests
{
    private static readonly string[] ExpectedAddInstructions =
        ["ldarg.0", "ldarg.1", "add", "ret"];

    [TestMethod]
    public async Task ArithmeticProgram_AfterRewrite_HasExactCoreILAndRunsWithoutPlaceholderDependency()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int Add(int left, int right)
                {
                    IL.Emit.Ldarg(left);
                    IL.Emit.Ldarg(right);
                    IL.Emit.Add();
                    return IL.Ret<int>();
                }

                public static int Main()
                {
                    Console.Write(Add(19, 23));
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("arithmetic");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "ArithmeticScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication,
            emitPdb: true);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);

        AssertRewriteSucceeded(rewrite);
        Assert.IsFalse(
            AssemblyInspector.ReferencesAssembly(
                artifact.AssemblyPath,
                "Nebulae.Runtime.Emit.Inline"));
        CollectionAssert.AreEqual(
            ExpectedAddInstructions,
            AssemblyInspector.GetMethodInstructions(
                artifact.AssemblyPath,
                "Scenario",
                "Add").ToArray());

        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    [TestMethod]
    public async Task LabelProgram_AfterRewrite_PreservesBranchSemantics()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int Choose(bool value)
                {
                    IL.Emit.Ldarg(value);
                    IL.Emit.Brfalse("false");
                    IL.Emit.Ldc_I4(1);
                    IL.Emit.Br("done");
                    IL.Label("false");
                    IL.Emit.Ldc_I4(2);
                    IL.Label("done");
                    return IL.Ret<int>();
                }

                public static int Main()
                {
                    Console.Write($"{Choose(true)},{Choose(false)}");
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("labels");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "LabelScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("1,2", execution.StandardOutput);
    }

    [TestMethod]
    public async Task GenericMethodReference_AfterRewrite_ResolvesAndRunsSelectedMethod()
    {
        const string source = """
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static IEnumerable<int> Empty()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Enumerable))
                            .Method(nameof(Enumerable.Empty), 1)
                            .MakeGeneric(typeof(int)));
                    return IL.Ret<IEnumerable<int>>();
                }

                public static int Main()
                {
                    Console.Write(Empty().Count());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("generic-reference");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "GenericReferenceScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("0", execution.StandardOutput);
    }

    [TestMethod]
    public async Task PushAndPop_WithTypeOfExpression_ReturnsTheRequestedSystemType()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static Type GetStringType()
                {
                    IL.Push(typeof(string));
                    return IL.Pop<Type>();
                }

                public static int Main()
                {
                    Console.Write(ReferenceEquals(GetStringType(), typeof(string)));
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("push-typeof");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "PushTypeOfScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("True", execution.StandardOutput);
    }

    [TestMethod]
    public async Task PushAndPop_WithCapturedValue_PreserveCompilerGeneratedValueFlow()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int ReadCaptured()
                {
                    int value = 42;
                    return Local();

                    int Local()
                    {
                        IL.Push(value);
                        return IL.Pop<int>();
                    }
                }

                public static int Main()
                {
                    Console.Write(ReadCaptured());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("push-pop-captured");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "PushPopCapturedScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    [TestMethod]
    public async Task PushAndPopRef_WithManagedReference_PreserveReferenceIdentity()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int UpdateThroughReference()
                {
                    int value = 41;
                    IL.Push(ref value);
                    ref int reference = ref IL.PopRef<int>();
                    reference++;
                    return value;
                }

                public static int Main()
                {
                    Console.Write(UpdateThroughReference());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("push-pop-reference");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "PushPopReferenceScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    [TestMethod]
    public async Task PushAndPopPointer_WithTypedAndVoidPointers_PreservePointerValues()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static unsafe int ReadPointers()
                {
                    int left = 19;
                    int right = 23;

                    IL.Push(&left);
                    int* typed = IL.PopPointer<int>();

                    IL.Push((void*)&right);
                    void* untyped = IL.PopPointer();

                    return *typed + *(int*)untyped;
                }

                public static int Main()
                {
                    Console.Write(ReadPointers());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("push-pop-pointer");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "PushPopPointerScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);
        AssertRewriteSucceeded(rewrite);
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    [TestMethod]
    public void MalformedConstantOperand_WhenRewriteFails_PreservesAssemblyAndSymbols()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int ReturnDynamic(int value)
                {
                    IL.Emit.Ldc_I4(value);
                    return IL.Ret<int>();
                }
            }
            """;
        using var directory = new TemporaryDirectory("malformed-constant");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "MalformedConstantScenario",
            directory.DirectoryPath,
            emitPdb: true,
            sourcePath: "MalformedConstantScenario.cs");
        string assemblyHash = AssemblyInspector.ComputeSha256(artifact.AssemblyPath);
        string pdbHash = AssemblyInspector.ComputeSha256(artifact.PdbPath!);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains("32-bit integer constant", rewrite.Errors[0].Message ?? string.Empty);
        Assert.EndsWith("MalformedConstantScenario.cs", rewrite.Errors[0].File);
        Assert.IsGreaterThan(0, rewrite.Errors[0].LineNumber);
        Assert.AreEqual(assemblyHash, AssemblyInspector.ComputeSha256(artifact.AssemblyPath));
        Assert.AreEqual(pdbHash, AssemblyInspector.ComputeSha256(artifact.PdbPath!));
    }

    [TestMethod]
    public void RewrittenAssembly_WhenTaskRunsAgain_IsSkippedAndRemainsByteIdentical()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int Value()
                {
                    IL.Emit.Ldc_I4(42);
                    return IL.Ret<int>();
                }
            }
            """;
        using var directory = new TemporaryDirectory("idempotent");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "IdempotentScenario",
            directory.DirectoryPath);
        AssertRewriteSucceeded(InlineILTaskHarness.Execute(artifact));
        string hash = AssemblyInspector.ComputeSha256(artifact.AssemblyPath);

        InlineILTaskResult secondRun = InlineILTaskHarness.Execute(artifact);

        AssertRewriteSucceeded(secondRun);
        Assert.IsTrue(secondRun.Messages.Any(static message =>
            message.Message?.Contains(
                "Skipping rewritten assembly",
                StringComparison.Ordinal) == true));
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(artifact.AssemblyPath));
    }

    [TestMethod]
    public void MemberReference_WhenMemberExistsOnlyOnBaseType_RejectsDeclaredTypeLookupAndPreservesInput()
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public class Base
            {
                public static void Target()
                {
                }
            }

            public sealed class Derived : Base
            {
            }

            public static class Scenario
            {
                public static void CallTarget()
                {
                    IL.Emit.Call(
                        IL.Ref(typeof(Derived))
                            .Method(nameof(Base.Target)));
                    IL.Emit.Ret();
                }
            }
            """;
        using var directory = new TemporaryDirectory("declared-member-only");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "DeclaredMemberScenario",
            directory.DirectoryPath);
        string hash = AssemblyInspector.ComputeSha256(artifact.AssemblyPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains(
            "Cannot find method named 'Target'",
            rewrite.Errors[0].Message ?? string.Empty);
        Assert.Contains("Derived", rewrite.Errors[0].Message ?? string.Empty);
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(artifact.AssemblyPath));
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
        string assemblyName = typeof(InlineRewriteContractTests).Assembly.GetName().Name!;
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
