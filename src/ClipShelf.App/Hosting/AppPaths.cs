namespace ClipShelf.App.Hosting;

// All user data lives under %LOCALAPPDATA%\ClipShelf. Nothing is written next to the exe.
public static class AppPaths
{
    public static string Root { get; } = Ensure(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipShelf"));

    public static string Database { get; } = Path.Combine(Root, "data.db");

    public static string DatabaseKey { get; } = Path.Combine(Root, "key.bin");

    public static string Settings { get; } = Path.Combine(Root, "settings.json");

    public static string Logs { get; } = Ensure(Path.Combine(Root, "logs"));

    public static string Backups { get; } = Ensure(Path.Combine(Root, "backups"));

    private static string Ensure(string directory) => Directory.CreateDirectory(directory).FullName;
}
