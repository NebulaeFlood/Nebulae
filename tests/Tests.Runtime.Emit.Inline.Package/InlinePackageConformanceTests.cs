using System.IO.Compression;
using Tests.Runtime.Emit.Inline.Infrastructure;

namespace Tests.Runtime.Emit.Inline.Package;

[TestClass]
[DoNotParallelize]
public sealed class InlinePackageConformanceTests
{
    [ClassInitialize]
    public static async Task CreatePackageAsync(TestContext _)
    {
        _workspace = new TemporaryDirectory("package");
        _packageDirectory = _workspace.GetPath("packages");
        Directory.CreateDirectory(_packageDirectory);

        string? suppliedPackagePath = Environment.GetEnvironmentVariable(
            PackagePathEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(suppliedPackagePath))
        {
            string fullPackagePath = System.IO.Path.GetFullPath(suppliedPackagePath);
            if (!File.Exists(fullPackagePath))
            {
                throw new FileNotFoundException(
                    "The supplied Inline IL package does not exist.",
                    fullPackagePath);
            }

            string? suppliedPackageVersion = Environment.GetEnvironmentVariable(
                PackageVersionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(suppliedPackageVersion))
            {
                throw new InvalidOperationException(
                    $"{PackageVersionEnvironmentVariable} must be set when " +
                    $"{PackagePathEnvironmentVariable} is supplied.");
            }

            _packageVersion = suppliedPackageVersion;
            _packagePath = _workspace.GetPath("packages", System.IO.Path.GetFileName(fullPackagePath));
            File.Copy(fullPackagePath, _packagePath);
            return;
        }

        _packageVersion = DefaultPackageVersion;

        ProcessResult pack = await ProcessRunner.RunAsync(
            "dotnet",
            [
                "pack",
                RepositoryPaths.InlineProject,
                "-c",
                "Debug",
                "--no-restore",
                "--output",
                _packageDirectory,
                $"-p:PackageVersion={_packageVersion}",
                "-p:IncludeSymbols=false"
            ],
            RepositoryPaths.Root,
            TimeSpan.FromMinutes(5));

        if (pack.ExitCode is not 0)
        {
            throw new InvalidOperationException(
                $"Package creation failed.{Environment.NewLine}" +
                $"stdout:{Environment.NewLine}{pack.StandardOutput}{Environment.NewLine}" +
                $"stderr:{Environment.NewLine}{pack.StandardError}");
        }

        _packagePath = Directory.GetFiles(
                _packageDirectory,
                "Nebulae.Runtime.Emit.Inline.*.nupkg",
                SearchOption.TopDirectoryOnly)
            .Single(static path => !path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase));
    }

    [ClassCleanup]
    public static void CleanupPackage()
    {
        _workspace?.Dispose();
    }

    [TestMethod]
    public void NuGetPackage_Contents_ContainRequiredAssetsWithoutRuntimeLibrary()
    {
        using ZipArchive package = ZipFile.OpenRead(_packagePath);
        string[] entries = [.. package.Entries.Select(static entry => entry.FullName.Replace('\\', '/'))];

        Assert.HasCount(1, entries.Where(static entry => entry ==
            "ref/netstandard2.0/Nebulae.Runtime.Emit.Inline.dll"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "ref/net10.0/Nebulae.Runtime.Emit.Inline.dll"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "analyzers/dotnet/cs/Nebulae.Runtime.Emit.Inline.Analyzers.dll"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "analyzers/dotnet/cs/zh-CN/Nebulae.Runtime.Emit.Inline.Analyzers.resources.dll"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "build/Nebulae.Runtime.Emit.Inline.targets"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "buildMultiTargeting/Nebulae.Runtime.Emit.Inline.targets"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "tasks/netstandard2.0/Nebulae.Runtime.Emit.Inline.MSBuild.dll"));
        Assert.HasCount(1, entries.Where(static entry => entry ==
            "tasks/net10.0/Nebulae.Runtime.Emit.Inline.MSBuild.dll"));
        Assert.IsFalse(entries.Any(static entry => entry.StartsWith("lib/", StringComparison.Ordinal)));
    }

    [TestMethod]
    [DataRow("net8.0")]
    [DataRow("net10.0")]
    public async Task PackageConsumer_WithPrivateAssetsAll_RewritesAndRunsWithoutRuntimeDependency(
        string targetFramework)
    {
        string projectDirectory = CreateConsumerProject(
            $"valid-{targetFramework}",
            targetFramework,
            privateAssets: "all");
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projectPath, "-c", "Release", "--no-restore"],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("build", build);

        string outputDirectory = System.IO.Path.Combine(
            projectDirectory,
            "bin",
            "Release",
            targetFramework);
        string assemblyPath = System.IO.Path.Combine(outputDirectory, "Consumer.dll");
        ProcessResult execution = await ProcessRunner.RunAsync(
            "dotnet",
            [assemblyPath],
            outputDirectory,
            TimeSpan.FromSeconds(30));

        AssertProcessSucceeded("run", execution);
        Assert.AreEqual("42", execution.StandardOutput);
        Assert.IsFalse(
            AssemblyInspector.ReferencesAssembly(
                assemblyPath,
                "Nebulae.Runtime.Emit.Inline"));
        Assert.IsFalse(File.Exists(System.IO.Path.Combine(
            outputDirectory,
            "Nebulae.Runtime.Emit.Inline.dll")));
    }

    [TestMethod]
    public async Task PackageConsumer_WithoutPrivateAssetsAll_FailsWithPackageBoundaryDiagnostic()
    {
        string projectDirectory = CreateConsumerProject(
            "missing-private-assets",
            "net10.0",
            privateAssets: null);
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projectPath, "-c", "Release", "--no-restore"],
            projectDirectory,
            TimeSpan.FromMinutes(3));

        Assert.AreNotEqual(0, build.ExitCode);
        Assert.Contains(
            "must set PrivateAssets='all'",
            build.StandardOutput + build.StandardError);
    }

    [TestMethod]
    public async Task PackageAnalyzer_InvalidConsumer_ReportsCurrentDiagnosticFromInstalledPackage()
    {
        const string program = """
            using Nebulae.Runtime.Emit.Inline;

            object escaped = IL.Ref(typeof(string));
            System.GC.KeepAlive(escaped);
            """;
        string projectDirectory = CreateConsumerProject(
            "packaged-analyzer",
            "net10.0",
            privateAssets: "all",
            program);
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projectPath, "-c", "Release", "--no-restore"],
            projectDirectory,
            TimeSpan.FromMinutes(3));

        Assert.AreNotEqual(0, build.ExitCode);
        Assert.Contains("NEBIL3001", build.StandardOutput + build.StandardError);
    }

    [TestMethod]
    [DataRow("net8.0")]
    [DataRow("net10.0")]
    public async Task PackageConsumer_DelegateFunctionPointerScenario_RewritesAndRuns(
        string targetFramework)
    {
        const string program = """
            using System;
            using System.Reflection;
            using Nebulae.Runtime.Emit.Inline;

            public delegate void Handler(int value);

            public static class Program
            {
                private static Handler Create(nint functionPointer)
                {
                    IL.Emit.Ldnull();
                    IL.Emit.Ldarg(functionPointer);
                    IL.Emit.Newobj(
                        IL.Ref(typeof(Handler))
                            .Constructor(typeof(object), typeof(nint)));
                    return IL.Ret<Handler>();
                }

                private static void Target(int value) => Console.Write(value);

                public static void Main()
                {
                    MethodInfo method = typeof(Program).GetMethod(
                        nameof(Target),
                        BindingFlags.Static | BindingFlags.NonPublic)!;
                    Create(method.MethodHandle.GetFunctionPointer())(42);
                }
            }
            """;
        string projectDirectory = CreateConsumerProject(
            $"delegate-{targetFramework}",
            targetFramework,
            privateAssets: "all",
            program);
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        string assemblyPath = await BuildConsumerAsync(
            projectPath,
            projectDirectory,
            targetFramework);
        ProcessResult execution = await RunConsumerAsync(
            assemblyPath,
            System.IO.Path.GetDirectoryName(assemblyPath)!);

        AssertProcessSucceeded("run", execution);
        Assert.AreEqual("42", execution.StandardOutput);
        AssertConsumerHasNoRuntimePlaceholderDependency(assemblyPath);
    }

    [TestMethod]
    [DataRow("net8.0")]
    [DataRow("net10.0")]
    public async Task PackageConsumer_AfterPublish_RunsWithoutRuntimePlaceholderDependency(
        string targetFramework)
    {
        string projectDirectory = CreateConsumerProject(
            $"publish-{targetFramework}",
            targetFramework,
            privateAssets: "all");
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");
        string publishDirectory = System.IO.Path.Combine(projectDirectory, "publish");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult publish = await ProcessRunner.RunAsync(
            "dotnet",
            [
                "publish",
                projectPath,
                "-c",
                "Release",
                "--no-restore",
                "--output",
                publishDirectory,
            ],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("publish", publish);
        string assemblyPath = System.IO.Path.Combine(publishDirectory, "Consumer.dll");

        ProcessResult execution = await RunConsumerAsync(assemblyPath, publishDirectory);

        AssertProcessSucceeded("run", execution);
        Assert.AreEqual("42", execution.StandardOutput);
        AssertConsumerHasNoRuntimePlaceholderDependency(assemblyPath);
    }

    [TestMethod]
    public async Task PackageConsumer_WhenNetstandardTaskIsForced_LoadsAndRunsThatTaskAsset()
    {
        const string reportTarget = """
              <Target Name="ReportInlineTaskFramework" AfterTargets="InlineIL">
                <Message Text="INLINE_TASK_TFM=$(_TaskTargetFramework)" Importance="High" />
              </Target>
            """;
        string projectDirectory = CreateConsumerProject(
            "netstandard-task",
            "net8.0",
            privateAssets: "all",
            program: ArithmeticProgram,
            additionalProjectContent: reportTarget);
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            [
                "build",
                projectPath,
                "-c",
                "Release",
                "--no-restore",
                "-p:_TaskTargetFramework=netstandard2.0",
            ],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("build", build);
        Assert.Contains("INLINE_TASK_TFM=netstandard2.0", build.StandardOutput);
        string assemblyPath = System.IO.Path.Combine(
            projectDirectory,
            "bin",
            "Release",
            "net8.0",
            "Consumer.dll");

        ProcessResult execution = await RunConsumerAsync(
            assemblyPath,
            System.IO.Path.GetDirectoryName(assemblyPath)!);

        AssertProcessSucceeded("run", execution);
        Assert.AreEqual("42", execution.StandardOutput);
        AssertConsumerHasNoRuntimePlaceholderDependency(assemblyPath);
    }

    [TestMethod]
    public async Task MultiTargetPackageConsumer_RewritesEveryInnerBuild()
    {
        const string program = """
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
            }
            """;
        string projectDirectory = CreateConsumerProject(
            "multi-target",
            "net8.0;net10.0",
            privateAssets: "all",
            program,
            outputType: null,
            targetFrameworkProperty: "TargetFrameworks");
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projectPath, "-c", "Release", "--no-restore"],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("build", build);

        foreach (string targetFramework in new[] { "net8.0", "net10.0" })
        {
            string assemblyPath = System.IO.Path.Combine(
                projectDirectory,
                "bin",
                "Release",
                targetFramework,
                "Consumer.dll");
            AssertConsumerHasNoRuntimePlaceholderDependency(assemblyPath);
        }
    }

    [TestMethod]
    public async Task DesignTimeBuild_LeavesCompilerOutputUnrewritten()
    {
        string projectDirectory = CreateConsumerProject(
            "design-time",
            "net10.0",
            privateAssets: "all");
        string projectPath = System.IO.Path.Combine(projectDirectory, "Consumer.csproj");

        ProcessResult restore = await RestoreAsync(projectPath, projectDirectory);
        AssertProcessSucceeded("restore", restore);
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            [
                "build",
                projectPath,
                "-c",
                "Debug",
                "--no-restore",
                "-p:DesignTimeBuild=true",
                "-p:SkipCompilerExecution=false",
            ],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("design-time build", build);
        string assemblyPath = System.IO.Path.Combine(
            projectDirectory,
            "obj",
            "Debug",
            "net10.0",
            "Consumer.dll");

        Assert.IsTrue(File.Exists(assemblyPath));
        Assert.IsTrue(AssemblyInspector.ReferencesAssembly(
            assemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
    }

    private static string CreateConsumerProject(
        string name,
        string targetFramework,
        string? privateAssets)
    {
        return CreateConsumerProject(
            name,
            targetFramework,
            privateAssets,
            ArithmeticProgram);
    }

    private static string CreateConsumerProject(
        string name,
        string targetFramework,
        string? privateAssets,
        string program,
        string? outputType = "Exe",
        string targetFrameworkProperty = "TargetFramework",
        string additionalProjectContent = "")
    {
        string projectDirectory = _workspace!.GetPath("consumers", name);
        Directory.CreateDirectory(projectDirectory);
        string privateAssetsElement = privateAssets is null
            ? string.Empty
            : $"<PrivateAssets>{privateAssets}</PrivateAssets>";
        string outputTypeElement = outputType is null
            ? string.Empty
            : $"<OutputType>{outputType}</OutputType>";
        string project = $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                {{outputTypeElement}}
                <{{targetFrameworkProperty}}>{{targetFramework}}</{{targetFrameworkProperty}}>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Nebulae.Runtime.Emit.Inline" Version="{{_packageVersion}}">
                  {{privateAssetsElement}}
                </PackageReference>
              </ItemGroup>
            {{additionalProjectContent}}
            </Project>
            """;

        File.WriteAllText(System.IO.Path.Combine(projectDirectory, "Consumer.csproj"), project);
        File.WriteAllText(System.IO.Path.Combine(projectDirectory, "Program.cs"), program);
        return projectDirectory;
    }

    private static async Task<string> BuildConsumerAsync(
        string projectPath,
        string projectDirectory,
        string targetFramework)
    {
        ProcessResult build = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", projectPath, "-c", "Release", "--no-restore"],
            projectDirectory,
            TimeSpan.FromMinutes(3));
        AssertProcessSucceeded("build", build);
        return System.IO.Path.Combine(
            projectDirectory,
            "bin",
            "Release",
            targetFramework,
            "Consumer.dll");
    }

    private static Task<ProcessResult> RunConsumerAsync(
        string assemblyPath,
        string workingDirectory)
    {
        return ProcessRunner.RunAsync(
            "dotnet",
            [assemblyPath],
            workingDirectory,
            TimeSpan.FromSeconds(30));
    }

    private static void AssertConsumerHasNoRuntimePlaceholderDependency(
        string assemblyPath)
    {
        Assert.IsFalse(AssemblyInspector.ReferencesAssembly(
            assemblyPath,
            "Nebulae.Runtime.Emit.Inline"));
        Assert.IsFalse(File.Exists(System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(assemblyPath)!,
            "Nebulae.Runtime.Emit.Inline.dll")));
    }

    private static Task<ProcessResult> RestoreAsync(
        string projectPath,
        string projectDirectory)
    {
        string packagesPath = _workspace!.GetPath("global-packages");
        return ProcessRunner.RunAsync(
            "dotnet",
            [
                "restore",
                projectPath,
                "--source",
                _packageDirectory,
                "--packages",
                packagesPath,
                "--no-cache"
            ],
            projectDirectory,
            TimeSpan.FromMinutes(3));
    }

    private static void AssertProcessSucceeded(string operation, ProcessResult result)
    {
        Assert.AreEqual(
            0,
            result.ExitCode,
            $"{operation} failed.{Environment.NewLine}" +
            $"stdout:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}" +
            $"stderr:{Environment.NewLine}{result.StandardError}");
    }

    private const string DefaultPackageVersion = "0.0.0-conformance";
    private const string ArithmeticProgram = """
        using Nebulae.Runtime.Emit.Inline;

        static int Add(int left, int right)
        {
            IL.Emit.Ldarg(left);
            IL.Emit.Ldarg(right);
            IL.Emit.Add();
            return IL.Ret<int>();
        }

        Console.Write(Add(19, 23));
        """;
    private const string PackagePathEnvironmentVariable = "NEBULAE_INLINE_IL_PACKAGE_PATH";
    private const string PackageVersionEnvironmentVariable = "NEBULAE_INLINE_IL_PACKAGE_VERSION";
    private static TemporaryDirectory? _workspace;
    private static string _packageDirectory = null!;
    private static string _packagePath = null!;
    private static string _packageVersion = null!;
}
