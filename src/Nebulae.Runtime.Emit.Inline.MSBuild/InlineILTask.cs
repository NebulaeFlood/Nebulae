using Microsoft.Build.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nebulae.Runtime.Emit.Inline.MSBuild
{
    public sealed class InlineILTask : Microsoft.Build.Utilities.Task
    {
        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        [Required]
        public string AssemblyPath { get; set; } = string.Empty;

        public string DebugType { get; set; } = string.Empty;

        public string[] References { get; set; } = [];

        #endregion


        public override bool Execute()
        {
            // System.Diagnostics.Debugger.Launch();

            try
            {
                bool readSymbols = !string.IsNullOrEmpty(DebugType) && !DebugType.Equals("none", StringComparison.OrdinalIgnoreCase);

                using var resolver = CreateResolver();
                using var assembly = AssemblyDefinition.ReadAssembly(AssemblyPath, new ReaderParameters(ReadingMode.Immediate)
                {
                    AssemblyResolver = resolver,
                    InMemory = true,
                    ReadSymbols = readSymbols
                });

                int count = AssemblyProcessor.Process(assembly, Log);

                assembly.Write(AssemblyPath, new WriterParameters
                {
                    WriteSymbols = readSymbols
                });

                Log.LogMessage(MessageImportance.High, $"Successfully processed {count} method(s) in assembly '{assembly.Name.Name}'.");
                return true;
            }
            catch (AggregateException e)
            {
                foreach (var inner in e.Flatten().InnerExceptions)
                {
                    LogError(inner);
                }

                return false;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }


        private DefaultAssemblyResolver CreateResolver()
        {
            var directories = new HashSet<string>(StringComparer.Ordinal);
            var directory = Path.GetDirectoryName(AssemblyPath);

            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                directories.Add(directory);
            }

            directory = Path.GetDirectoryName(typeof(InlineILTask).Assembly.Location);

            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                directories.Add(directory);
            }

            foreach (var reference in References)
            {
                if (!string.IsNullOrWhiteSpace(reference) && Directory.Exists(reference))
                {
                    directories.Add(reference);
                }
            }

            var resolver = new DefaultAssemblyResolver();

            foreach (var dir in directories)
            {
                resolver.AddSearchDirectory(dir);
            }

            return resolver;
        }

        private void LogError(Exception e)
        {
            string message = e.Message;

            for (var inner = e.InnerException; inner is not null; inner = inner.InnerException)
            {
                message += Environment.NewLine + inner.Message;
            }

            if (e.Data.Contains(nameof(SequencePoint)))
            {
                var point = (SequencePoint)e.Data[nameof(SequencePoint)]!;

                Log.LogError(
                    subcategory: null,
                    errorCode: null,
                    helpKeyword: null,
                    file: point.Document.Url,
                    lineNumber: point.StartLine,
                    columnNumber: point.StartColumn,
                    endLineNumber: point.EndLine,
                    endColumnNumber: point.EndColumn,
                    message: message);

                e.Data.Remove(nameof(SequencePoint));
            }
            else
            {
                Log.LogError(message);
            }
        }
    }
}
