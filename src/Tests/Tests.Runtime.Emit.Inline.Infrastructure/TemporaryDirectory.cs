namespace Tests.Runtime.Emit.Inline.Infrastructure;

public sealed class TemporaryDirectory : IDisposable
{
    public string DirectoryPath { get; }

    public TemporaryDirectory(string? name = null)
    {
        string root = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "Nebulae.Runtime.Emit.Inline.Tests");
        string leaf = string.IsNullOrWhiteSpace(name)
            ? Guid.NewGuid().ToString("N")
            : $"{name}-{Guid.NewGuid():N}";

        DirectoryPath = System.IO.Path.Combine(root, leaf);
        Directory.CreateDirectory(DirectoryPath);
    }

    public string GetPath(params string[] segments)
    {
        string result = DirectoryPath;

        foreach (string segment in segments)
        {
            result = System.IO.Path.Combine(result, segment);
        }

        return result;
    }

    public void Dispose()
    {
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}
