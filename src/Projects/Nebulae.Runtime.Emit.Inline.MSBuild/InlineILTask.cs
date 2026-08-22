using Microsoft.Build.Framework;
using Mono.Cecil;
using Nebulae.Runtime.Emit.Inline.MSBuild.Helpers;
using Nebulae.Runtime.Emit.Inline.MSBuild.Rewrite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        public string KeyOriginatorFile { get; set; } = string.Empty;

        public bool PublicSign { get; set; }

        public string[] References { get; set; } = [];

        #endregion


        public override bool Execute()
        {
            // System.Diagnostics.Debugger.Launch();

            try
            {
                if (!AssemblyRewriteContext.RequiresRewrite(this, out string assemblyName))
                {
                    Log.LogMessage(
                        MessageImportance.High,
                        $"Skipping rewritten assembly '{assemblyName}'.");
                    return true;
                }

                AssemblyRewriter.Rewrite(this);
                return true;
            }
            catch (AggregateException e)
            {
                var exceptions = e.Flatten().InnerExceptions
                    .OrderBy(e => e.Message, StringComparer.Ordinal);

                foreach (var exception in exceptions)
                {
                    LogError(exception);
                }

                return false;
            }
            catch (Exception e)
            {
                LogError(e);
                return false;
            }
        }

        private void LogError(Exception e)
        {
            string message = e.Message;

            for (var inner = e.InnerException; inner is not null; inner = inner.InnerException)
            {
                message += Environment.NewLine + inner.Message;
            }

            if (e.TryGetFileInfo(out string file, out int startLine, out int startColumn, out int endLine, out int endColumn))
            {
                Log.LogError(
                    subcategory: null,
                    errorCode: null,
                    helpKeyword: null,
                    file: file,
                    lineNumber: startLine,
                    columnNumber: startColumn,
                    endLineNumber: endLine,
                    endColumnNumber: endColumn,
                    message: message);
            }
            else
            {
                Log.LogError(message);
            }
        }
    }
}
