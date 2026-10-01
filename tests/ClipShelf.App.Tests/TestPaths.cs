namespace ClipShelf.App.Tests;

// Locates source files from the test output folder. Shared by every file-scanning test.
internal static class TestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string AppProject => Path.Combine(RepoRoot, "src", "ClipShelf.App");

    public static IEnumerable<string> SourceFiles(params string[] extensions)
    {
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        return Directory.EnumerateFiles(Path.Combine(RepoRoot, "src"), "*.*", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal))
            .Where(f => extensions.Any(e => f.EndsWith(e, StringComparison.Ordinal)));
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "ClipShelf.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Repository root not found.");
    }
}
