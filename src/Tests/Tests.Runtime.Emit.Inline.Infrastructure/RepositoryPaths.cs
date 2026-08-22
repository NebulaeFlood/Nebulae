namespace Tests.Runtime.Emit.Inline.Infrastructure;

public static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string InlineProject => System.IO.Path.Combine(
        Root,
        "src",
        "Projects",
        "Nebulae.Runtime.Emit.Inline",
        "Nebulae.Runtime.Emit.Inline.csproj");

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "Nebulae.slnx"))
                && Directory.Exists(System.IO.Path.Combine(directory.FullName, "src", "Projects")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Cannot locate the Nebulae repository from '{AppContext.BaseDirectory}'.");
    }
}
