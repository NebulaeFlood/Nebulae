using System.Diagnostics;
using System.Reflection;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal static class BuildScenarioTestHelpers
{
    private const string ScenarioProjectName = "InlineILFailureScenario.csproj";

    public static async Task<BuildScenarioResult> BuildFailureScenarioAsync(
        string scenarioName,
        string debugType = "portable")
    {
        string scenarioDirectory = GetScenarioDirectory();
        string temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            string taskTargetFramework = GetAssemblyMetadata("InlineILTaskTargetFramework");
            string configuration = GetAssemblyMetadata("InlineILBuildConfiguration");
            string repositoryRoot = GetAssemblyMetadata("InlineILRepositoryRoot");
            string projectDirectory = Path.Combine(repositoryRoot, "src", "Projects");
            string inlineAssemblyPath = Path.Combine(
                projectDirectory,
                "Nebulae.Runtime.Emit.Inline",
                "bin",
                configuration,
                taskTargetFramework,
                "Nebulae.Runtime.Emit.Inline.dll");
            string taskAssemblyPath = Path.Combine(
                projectDirectory,
                "Nebulae.Runtime.Emit.Inline.MSBuild",
                "bin",
                configuration,
                taskTargetFramework,
                "Nebulae.Runtime.Emit.Inline.MSBuild.dll");

            Assert.IsTrue(File.Exists(inlineAssemblyPath), $"Inline IL assembly was not found at '{inlineAssemblyPath}'.");
            Assert.IsTrue(File.Exists(taskAssemblyPath), $"Inline IL task assembly was not found at '{taskAssemblyPath}'.");

            string scenarioSource = Path.Combine(scenarioDirectory, scenarioName + ".cs");
            Assert.IsTrue(File.Exists(scenarioSource), $"Build scenario source was not found at '{scenarioSource}'.");

            return await RunDotNetAsync(
                scenarioDirectory,
                [
                    "build",
                    Path.Combine(scenarioDirectory, ScenarioProjectName),
                    "--nologo",
                    "--disable-build-servers",
                    "--verbosity",
                    "minimal",
                    $"-p:ScenarioTargetFramework={GetConsumerTargetFramework()}",
                    $"-p:ScenarioSource={scenarioSource}",
                    $"-p:ScenarioDebugType={debugType}",
                    $"-p:InlineAssemblyPath={inlineAssemblyPath}",
                    $"-p:InlineTaskAssemblyPath={taskAssemblyPath}",
                    $"-p:BaseOutputPath={Path.Combine(temporaryDirectory, "bin")}{Path.DirectorySeparatorChar}",
                    $"-p:BaseIntermediateOutputPath={Path.Combine(temporaryDirectory, "obj")}{Path.DirectorySeparatorChar}",
                ]);
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    public static async Task<RewrittenProbeResult> RewriteGeneratedProbeAsync(string source)
    {
        string scenarioDirectory = GetScenarioDirectory();
        string temporaryDirectory = CreateTemporaryDirectory();

        try
        {
            string scenarioSource = Path.Combine(temporaryDirectory, "PrimitiveReplacementProbes.cs");
            await File.WriteAllTextAsync(scenarioSource, source);

            string taskTargetFramework = GetAssemblyMetadata("InlineILTaskTargetFramework");
            string configuration = GetAssemblyMetadata("InlineILBuildConfiguration");
            string repositoryRoot = GetAssemblyMetadata("InlineILRepositoryRoot");
            string projectDirectory = Path.Combine(repositoryRoot, "src", "Projects");
            string inlineAssemblyPath = Path.Combine(
                projectDirectory,
                "Nebulae.Runtime.Emit.Inline",
                "bin",
                configuration,
                taskTargetFramework,
                "Nebulae.Runtime.Emit.Inline.dll");
            string taskAssemblyPath = Path.Combine(
                projectDirectory,
                "Nebulae.Runtime.Emit.Inline.MSBuild",
                "bin",
                configuration,
                taskTargetFramework,
                "Nebulae.Runtime.Emit.Inline.MSBuild.dll");

            Assert.IsTrue(File.Exists(inlineAssemblyPath), $"Inline IL assembly was not found at '{inlineAssemblyPath}'.");
            Assert.IsTrue(File.Exists(taskAssemblyPath), $"Inline IL task assembly was not found at '{taskAssemblyPath}'.");

            BuildScenarioResult buildResult = await RunDotNetAsync(
                scenarioDirectory,
                [
                    "build",
                    Path.Combine(scenarioDirectory, ScenarioProjectName),
                    "--nologo",
                    "--disable-build-servers",
                    "--verbosity",
                    "minimal",
                    $"-p:ScenarioTargetFramework={GetConsumerTargetFramework()}",
                    $"-p:ScenarioSource={scenarioSource}",
                    "-p:ScenarioDebugType=none",
                    "-p:Optimize=true",
                    $"-p:InlineAssemblyPath={inlineAssemblyPath}",
                    $"-p:InlineTaskAssemblyPath={taskAssemblyPath}",
                    $"-p:BaseOutputPath={Path.Combine(temporaryDirectory, "bin")}{Path.DirectorySeparatorChar}",
                    $"-p:BaseIntermediateOutputPath={Path.Combine(temporaryDirectory, "obj")}{Path.DirectorySeparatorChar}",
                ]);

            if (buildResult.ExitCode != 0)
            {
                return new RewrittenProbeResult(buildResult.ExitCode, buildResult.Output, null);
            }

            string[] assemblies = Directory.GetFiles(
                Path.Combine(temporaryDirectory, "bin"),
                "InlineILFailureScenario.dll",
                SearchOption.AllDirectories);
            Assert.HasCount(
                1,
                assemblies,
                $"Expected one rewritten probe assembly, but found {assemblies.Length}.{Environment.NewLine}{buildResult.Output}");

            return new RewrittenProbeResult(
                buildResult.ExitCode,
                buildResult.Output,
                await File.ReadAllBytesAsync(assemblies[0]));
        }
        finally
        {
            TryDeleteDirectory(temporaryDirectory);
        }
    }

    public static async Task<BuildScenarioResult> ValidatePrivateAssetsAsync()
    {
        string scenarioDirectory = GetScenarioDirectory();
        string repositoryRoot = GetAssemblyMetadata("InlineILRepositoryRoot");
        string targetsPath = Path.Combine(
            repositoryRoot,
            "src",
            "Projects",
            "Nebulae.Runtime.Emit.Inline",
            "Assets",
            "buildMultiTargeting",
            "Nebulae.Runtime.Emit.Inline.targets");

        Assert.IsTrue(File.Exists(targetsPath), $"Inline IL targets file was not found at '{targetsPath}'.");

        return await RunDotNetAsync(
            scenarioDirectory,
            [
                "msbuild",
                Path.Combine(scenarioDirectory, "PrivateAssetsValidation.proj"),
                "-nologo",
                "-t:InlineIL",
                $"-p:InlineTargetsPath={targetsPath}",
            ]);
    }

    private static async Task<BuildScenarioResult> RunDotNetAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        Assert.IsTrue(process.Start(), "The dotnet process could not be started.");

        Task<string> standardOutput = process.StandardOutput.ReadToEndAsync();
        Task<string> standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new BuildScenarioResult(
            process.ExitCode,
            (await standardOutput) + Environment.NewLine + (await standardError));
    }

    private static string GetScenarioDirectory()
    {
        return Path.Combine(
            GetAssemblyMetadata("InlineILRepositoryRoot"),
            "src",
            "Tests",
            "Tests.Runtime.Emit.Inline",
            "BuildScenarios");
    }

    private static string GetAssemblyMetadata(string key)
    {
        string? value = typeof(BuildScenarioTestHelpers).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .SingleOrDefault(attribute => attribute.Key == key)
            ?.Value;

        return string.IsNullOrWhiteSpace(value)
            ? throw new AssertFailedException($"Assembly metadata '{key}' was not generated.")
            : value;
    }

    private static string GetConsumerTargetFramework()
    {
#if NET10_0_OR_GREATER
        return "net10.0";
#else
        return "net8.0";
#endif
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "NebulaeInlineBuildScenarios", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}

internal sealed record BuildScenarioResult(int ExitCode, string Output);

internal sealed record RewrittenProbeResult(int ExitCode, string Output, byte[]? AssemblyBytes);
