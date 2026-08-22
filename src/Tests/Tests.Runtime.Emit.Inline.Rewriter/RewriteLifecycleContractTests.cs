using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Mono.Cecil;
using System.Security.Cryptography;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Rewriter;

[TestClass]
public sealed class RewriteLifecycleContractTests
{
    [TestMethod]
    public void PlaceholderReferenceWithoutRewriteTargets_IsRemovedAndSecondRunIsNoOp()
    {
        using var directory = new TemporaryDirectory("reference-only");
        string assemblyPath = CreateReferenceOnlyAssembly(
            directory.GetPath("ReferenceOnly.dll"));

        InlineILTaskResult firstRun = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        AssertRewriteSucceeded(firstRun);
        Assert.IsFalse(AssemblyInspector.ReferencesAssembly(
            assemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
        Assert.IsTrue(firstRun.Messages.Any(static message =>
            message.Message?.Contains(
                "Successfully removed reference",
                StringComparison.Ordinal) == true));
        string rewrittenHash = AssemblyInspector.ComputeSha256(assemblyPath);

        InlineILTaskResult secondRun = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        AssertRewriteSucceeded(secondRun);
        Assert.IsTrue(secondRun.Messages.Any(static message =>
            message.Message?.Contains(
                "Skipping rewritten assembly",
                StringComparison.Ordinal) == true));
        Assert.AreEqual(rewrittenHash, AssemblyInspector.ComputeSha256(assemblyPath));
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DebugSymbols_AfterSuccessfulRewrite_RemainReadableAndMapped(
        bool embedded)
    {
        const string source = """
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                public static int Value()
                {
                    int ordinaryLocal = 1;
                    System.GC.KeepAlive(ordinaryLocal);
                    IL.Emit.Ldc_I4(42);
                    return IL.Ret<int>();
                }
            }
            """;
        using var directory = new TemporaryDirectory(
            embedded ? "embedded-symbols" : "portable-symbols");
        DebugInformationFormat format = embedded
            ? DebugInformationFormat.Embedded
            : DebugInformationFormat.PortablePdb;
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            embedded ? "EmbeddedSymbolScenario" : "PortableSymbolScenario",
            directory.DirectoryPath,
            optimizationLevel: OptimizationLevel.Release,
            emitPdb: true,
            sourcePath: "SymbolScenario.cs",
            debugInformationFormat: format);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            artifact,
            debugType: embedded ? "embedded" : "portable");

        AssertRewriteSucceeded(rewrite);
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(
            artifact.AssemblyPath,
            new ReaderParameters { ReadSymbols = true });
        MethodDefinition method = assembly.MainModule
            .GetType("Scenario")!
            .Methods.Single(static candidate => candidate.Name == "Value");

        Assert.IsTrue(assembly.MainModule.HasSymbols);
        Assert.IsTrue(method.DebugInformation.HasSequencePoints);
        Assert.IsTrue(method.DebugInformation.SequencePoints.Any(static point =>
            point.Document.Url.EndsWith("SymbolScenario.cs", StringComparison.Ordinal)));
        Assert.AreEqual(embedded, artifact.PdbPath is null);
    }

    [TestMethod]
    public void StrongNamedAssembly_WithOriginatorKey_PreservesIdentityAndSignedState()
    {
        using var directory = new TemporaryDirectory("strong-name");
        byte[] keyBlob = CreateStrongNameKeyBlob();
        string keyPath = directory.GetPath("test.snk");
        File.WriteAllBytes(keyPath, keyBlob);
        string assemblyPath = CreateReferenceOnlyAssembly(
            directory.GetPath("SignedReferenceOnly.dll"),
            keyBlob);

#pragma warning disable IDE0042
        (byte[] token, bool signed) before = ReadStrongNameIdentity(assemblyPath);
#pragma warning restore IDE0042

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths,
            keyOriginatorFile: keyPath);

        AssertRewriteSucceeded(rewrite);

#pragma warning disable IDE0042
        (byte[] token, bool signed) after = ReadStrongNameIdentity(assemblyPath);
#pragma warning restore IDE0042

        Assert.IsTrue(before.signed);
        Assert.IsTrue(after.signed);
        CollectionAssert.AreEqual(before.token, after.token);
        Assert.IsFalse(AssemblyInspector.ReferencesAssembly(
            assemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
    }

    [TestMethod]
    public void StrongNamedAssembly_WithoutOriginatorKey_RejectsAndPreservesInput()
    {
        using var directory = new TemporaryDirectory("strong-name-missing-key");
        string assemblyPath = CreateReferenceOnlyAssembly(
            directory.GetPath("SignedReferenceOnly.dll"),
            CreateStrongNameKeyBlob());
        string hash = AssemblyInspector.ComputeSha256(assemblyPath);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(
            assemblyPath,
            CompilationHarness.DefaultReferencePaths);

        Assert.IsFalse(rewrite.Success);
        Assert.HasCount(1, rewrite.Errors);
        Assert.Contains("originator key file from empty path", rewrite.Errors[0].Message ?? string.Empty);
        Assert.AreEqual(hash, AssemblyInspector.ComputeSha256(assemblyPath));
    }

    [TestMethod]
    public async Task DirectRetExtension_WhenCompiledForDebug_ReturnsEmittedValue()
    {
        const string source = """
            using System;
            using Nebulae.Runtime.Emit.Inline;

            public static class Scenario
            {
                private static int Value()
                {
                    IL.Emit.Ldc_I4(42);
                    return IL.Ret<int>();
                }

                public static int Main()
                {
                    Console.Write(Value());
                    return 0;
                }
            }
            """;
        using var directory = new TemporaryDirectory("debug-ret");
        CompilationArtifact artifact = CompilationHarness.Compile(
            source,
            "DebugRetScenario",
            directory.DirectoryPath,
            OutputKind.ConsoleApplication,
            optimizationLevel: OptimizationLevel.Debug);

        InlineILTaskResult rewrite = InlineILTaskHarness.Execute(artifact);

        AssertRewriteSucceeded(rewrite);
        Assert.IsFalse(AssemblyInspector.ReferencesAssembly(
            artifact.AssemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
        ProcessResult execution = await RunRewrittenProgramAsync(artifact);

        Assert.AreEqual(0, execution.ExitCode, execution.StandardError);
        Assert.AreEqual("42", execution.StandardOutput);
    }

    private static string CreateReferenceOnlyAssembly(
        string assemblyPath,
        byte[]? strongNameKeyBlob = null)
    {
        string assemblyName = System.IO.Path.GetFileNameWithoutExtension(assemblyPath);
        using AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
            assemblyName,
            ModuleKind.Dll);
        assembly.MainModule.AssemblyReferences.Add(
            AssemblyNameReference.Parse(
                typeof(Nebulae.Runtime.Emit.Inline.IL).Assembly.FullName!));

        if (strongNameKeyBlob is null)
        {
            assembly.Write(assemblyPath);
        }
        else
        {
            assembly.Write(
                assemblyPath,
                new WriterParameters { StrongNameKeyBlob = strongNameKeyBlob });
        }

        return assemblyPath;
    }

    private static (byte[] Token, bool Signed) ReadStrongNameIdentity(string assemblyPath)
    {
        using AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath);
        bool signed = (assembly.MainModule.Attributes & ModuleAttributes.StrongNameSigned) != 0;
        return ([.. assembly.Name.PublicKeyToken], signed);
    }

    private static byte[] CreateStrongNameKeyBlob()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters parameters = rsa.ExportParameters(includePrivateParameters: true);
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        int modulusLength = parameters.Modulus!.Length;

        writer.Write((byte)0x07);
        writer.Write((byte)0x02);
        writer.Write((ushort)0);
        writer.Write(0x00002400u);
        writer.Write(0x32415352u);
        writer.Write((uint)(modulusLength * 8));
        writer.Write(ReadPublicExponent(parameters.Exponent!));
        WriteLittleEndian(writer, parameters.Modulus, modulusLength);
        WriteLittleEndian(writer, parameters.P, modulusLength / 2);
        WriteLittleEndian(writer, parameters.Q, modulusLength / 2);
        WriteLittleEndian(writer, parameters.DP, modulusLength / 2);
        WriteLittleEndian(writer, parameters.DQ, modulusLength / 2);
        WriteLittleEndian(writer, parameters.InverseQ, modulusLength / 2);
        WriteLittleEndian(writer, parameters.D, modulusLength);
        return stream.ToArray();
    }

    private static uint ReadPublicExponent(byte[] exponent)
    {
        uint result = 0;

        foreach (byte value in exponent)
        {
            result = (result << 8) | value;
        }

        return result;
    }

    private static void WriteLittleEndian(
        BinaryWriter writer,
        byte[]? value,
        int size)
    {
        byte[] result = new byte[size];

        for (int i = 0; i < value!.Length; i++)
        {
            result[i] = value[value.Length - i - 1];
        }

        writer.Write(result);
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
        string assemblyName = typeof(RewriteLifecycleContractTests).Assembly.GetName().Name!;
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
