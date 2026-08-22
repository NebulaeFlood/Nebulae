using System.Diagnostics;

namespace Tests.Runtime.Emit.Inline.Infrastructure;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

public static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        string workingDirectory,
        TimeSpan? timeout = null,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";

        if (environment is not null)
        {
            foreach ((string key, string? value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };

        if (!process.Start())
        {
            throw new InvalidOperationException($"Cannot start process '{fileName}'.");
        }

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(2));

        try
        {
            await process.WaitForExitAsync(cancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            await process.WaitForExitAsync().ConfigureAwait(false);
            throw new TimeoutException(
                $"Process '{fileName}' did not exit within '{timeout ?? TimeSpan.FromMinutes(2)}'.");
        }

        return new ProcessResult(
            process.ExitCode,
            await output.ConfigureAwait(false),
            await error.ConfigureAwait(false));
    }
}
