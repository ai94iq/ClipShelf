using System.Text;

namespace ClipShelf.Desktop.Platform.Linux;

// An XDG autostart entry (~/.config/autostart/clipshelf.desktop) that starts ClipShelf at sign-in.
// Applying every launch also repairs the path after the app moves.
public sealed class AutostartRegistration(
    ILogger<AutostartRegistration> log, string? configHome = null, Func<string?>? executable = null)
    : IStartupRegistration
{
    private string EntryPath => Path.Combine(
        configHome
            ?? (Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } xdg ? xdg
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")),
        "autostart", "clipshelf.desktop");

    public void Apply(bool enabled)
    {
        try
        {
            var path = EntryPath;
            if (!enabled)
            {
                if (File.Exists(path)) File.Delete(path);
                return;
            }

            var exe = (executable ?? (() => Environment.ProcessPath))();
            if (exe is null)
            {
                log.LogWarning("The executable path is unknown; the startup setting was not applied");
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, $"""
                [Desktop Entry]
                Type=Application
                Name=Copyloft
                Exec={Quote(exe)}
                Icon=clipshelf
                Terminal=false
                X-GNOME-Autostart-enabled=true

                """);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Applying the startup setting failed");
        }
    }

    // Quoting rule first (backslash before " ` $ \), then the string rule doubles each backslash;
    // % is doubled so it is never read as a field code.
    internal static string Quote(string path)
    {
        var quoted = new StringBuilder("\"");
        foreach (var c in path)
            quoted.Append(c switch
            {
                '"' or '`' or '$' => @"\\" + c,
                '\\' => @"\\\\",
                '%' => "%%",
                _ => c.ToString(),
            });
        return quoted.Append('"').ToString();
    }
}
