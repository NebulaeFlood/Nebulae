using System;

namespace Nebulae.Runtime.Emit.Inline.MSBuild.Helpers
{
    internal static class DiagnosticHelpers
    {
        public static T With<T>(this T exception, string key, object value) where T : Exception
        {
            exception.Data[key] = value;
            return exception;
        }
    }
}
