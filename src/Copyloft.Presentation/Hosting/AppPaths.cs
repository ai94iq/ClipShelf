namespace Copyloft.Presentation.Hosting;

// All user data lives under %LOCALAPPDATA%\Copyloft. Nothing is written next to the exe.
public static class AppPaths
{
    public static string Root { get; } = Ensure(MoveLegacyRoot(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Copyloft")));

    public static string Database { get; } = Path.Combine(Root, "data.db");

    public static string DatabaseKey { get; } = Path.Combine(Root, "key.bin");

    public static string Settings { get; } = Path.Combine(Root, "settings.json");

    public static string Logs { get; } = Ensure(Path.Combine(Root, "logs"));

    public static string Backups { get; } = Ensure(Path.Combine(Root, "backups"));

    // The app was ClipShelf up to 0.4.1. Its folder moves over once, so the history, key and settings
    // come along. While a running ClipShelf still holds it open, this run uses it in place and a
    // later start moves it.
    internal static string MoveLegacyRoot(string root)
    {
        var legacy = Path.Combine(Path.GetDirectoryName(root)!, "ClipShelf");
        if (Directory.Exists(root) || !Directory.Exists(legacy))
            return root;
        try
        {
            Directory.Move(legacy, root);
            return root;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return legacy;
        }
    }

    private static string Ensure(string directory) => Directory.CreateDirectory(directory).FullName;
}
