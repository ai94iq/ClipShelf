using System.Runtime.Versioning;

namespace ClipShelf.Desktop.Platform.Linux;

// Run-at-startup on Linux: a clipshelf.desktop entry under autostart. The entry
// launches without arguments, matching the Linux boot path (straight into
// history until the tray lands).
[SupportedOSPlatform("linux")]
public sealed class LinuxStartup(ILogger<LinuxStartup> log) : IStartupRegistration
{
    private const string FileName = "clipshelf.desktop";

    public void Apply(bool enabled)
    {
        var directory = AutostartDir();
        var file = Path.Combine(directory, FileName);
        if (!enabled)
        {
            if (File.Exists(file)) File.Delete(file);
            return;
        }

        var executable = Environment.ProcessPath;
        if (executable is null)
        {
            log.LogWarning("Run at startup requested but the executable path is unknown.");
            return;
        }

        Directory.CreateDirectory(directory);
        File.WriteAllText(file, Entry(executable));
    }

    internal static string AutostartDir()
    {
        var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrWhiteSpace(configHome))
            configHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        return Path.Combine(configHome, "autostart");
    }

    internal static string Entry(string executable) =>
        "[Desktop Entry]\n"
        + "Type=Application\n"
        + "Name=ClipShelf\n"
        + "Comment=Clipboard history that pastes back where you copied\n"
        + $"Exec=\"{executable}\"\n"
        + "Terminal=false\n"
        + "Categories=Utility;\n"
        + "X-GNOME-Autostart-enabled=true\n";
}
