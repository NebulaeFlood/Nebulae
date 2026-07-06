using Mono.Cecil;

namespace Tests.Runtime.Emit.Inline.Helpers;

internal static class AssemblyHelpers
{
    public static string GetCurrentTestAssemblyPath()
    {
        return typeof(AssemblyHelpers).Assembly.Location;
    }

    public static AssemblyDefinition ReadCurrentTestAssembly()
    {
        return AssemblyDefinition.ReadAssembly(
            GetCurrentTestAssemblyPath(),
            new ReaderParameters(ReadingMode.Immediate)
            {
                InMemory = true,
            });
    }
}
