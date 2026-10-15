namespace Copyloft.Presentation.Hosting;

// The app was ClipShelf up to 0.4.1. Its data folder moves over once, so the history, key and
// settings come along. While a running ClipShelf still holds it open, this run uses it in place and
// a later start moves it. Kept out of AppPaths so tests can run it on temp folders without
// AppPaths' initializer moving the real folder.
internal static class LegacyDataFolder
{
    public static string MoveTo(string root)
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
}
