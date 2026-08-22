using Microsoft.Build.Framework;
using Nebulae.Runtime.Emit.Inline.MSBuild;

namespace Tests.Runtime.Emit.Inline.Infrastructure;

public sealed record InlineILTaskResult(
    bool Success,
    IReadOnlyList<BuildErrorEventArgs> Errors,
    IReadOnlyList<BuildWarningEventArgs> Warnings,
    IReadOnlyList<BuildMessageEventArgs> Messages);

public static class InlineILTaskHarness
{
    public static InlineILTaskResult Execute(
        CompilationArtifact artifact,
        string? debugType = null,
        string? keyOriginatorFile = null,
        bool publicSign = false)
    {
        return Execute(
            artifact.AssemblyPath,
            artifact.ReferencePaths,
            debugType ?? (artifact.PdbPath is null ? "none" : "portable"),
            keyOriginatorFile,
            publicSign);
    }

    public static InlineILTaskResult Execute(
        string assemblyPath,
        IEnumerable<string> references,
        string debugType = "none",
        string? keyOriginatorFile = null,
        bool publicSign = false)
    {
        var buildEngine = new CapturingBuildEngine();
        var task = new InlineILTask
        {
            BuildEngine = buildEngine,
            AssemblyPath = assemblyPath,
            DebugType = debugType,
            KeyOriginatorFile = keyOriginatorFile ?? string.Empty,
            PublicSign = publicSign,
            References = [.. references]
        };

        bool success = task.Execute();
        return new InlineILTaskResult(
            success,
            buildEngine.Errors,
            buildEngine.Warnings,
            buildEngine.Messages);
    }

    private sealed class CapturingBuildEngine : IBuildEngine
    {
        public bool ContinueOnError => false;

        public int LineNumberOfTaskNode => 0;

        public int ColumnNumberOfTaskNode => 0;

        public string ProjectFileOfTaskNode => string.Empty;

        public IReadOnlyList<BuildErrorEventArgs> Errors
        {
            get
            {
                lock (_gate)
                {
                    return [.. _errors];
                }
            }
        }

        public IReadOnlyList<BuildWarningEventArgs> Warnings
        {
            get
            {
                lock (_gate)
                {
                    return [.. _warnings];
                }
            }
        }

        public IReadOnlyList<BuildMessageEventArgs> Messages
        {
            get
            {
                lock (_gate)
                {
                    return [.. _messages];
                }
            }
        }

        public void LogErrorEvent(BuildErrorEventArgs e)
        {
            lock (_gate)
            {
                _errors.Add(e);
            }
        }

        public void LogWarningEvent(BuildWarningEventArgs e)
        {
            lock (_gate)
            {
                _warnings.Add(e);
            }
        }

        public void LogMessageEvent(BuildMessageEventArgs e)
        {
            lock (_gate)
            {
                _messages.Add(e);
            }
        }

        public void LogCustomEvent(CustomBuildEventArgs e)
        {
        }

        public bool BuildProjectFile(
            string projectFileName,
            string[] targetNames,
            System.Collections.IDictionary globalProperties,
            System.Collections.IDictionary targetOutputs)
        {
            return false;
        }

        private readonly Lock _gate = new();
        private readonly List<BuildErrorEventArgs> _errors = [];
        private readonly List<BuildWarningEventArgs> _warnings = [];
        private readonly List<BuildMessageEventArgs> _messages = [];
    }
}
