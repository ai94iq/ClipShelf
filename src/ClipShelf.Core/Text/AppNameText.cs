namespace ClipShelf.Core.Text;

// Turns a process name into the app name shown beside a clip: "chrome" -> "Chrome",
// "msedge" -> "Edge", with a small map for the apps users see most.
public static class AppNameText
{
    private static readonly Dictionary<string, string> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chrome"] = "Chrome",
        ["msedge"] = "Edge",
        ["firefox"] = "Firefox",
        ["explorer"] = "File Explorer",
        ["notepad"] = "Notepad",
        ["winword"] = "Word",
        ["excel"] = "Excel",
        ["powerpnt"] = "PowerPoint",
        ["outlook"] = "Outlook",
        ["devenv"] = "Visual Studio",
        ["code"] = "VS Code",
        ["windowsterminal"] = "Windows Terminal",
        ["teams"] = "Teams",
        ["ms-teams"] = "Teams",
        ["telegram"] = "Telegram",
        ["discord"] = "Discord",
        ["spotify"] = "Spotify",
        ["powershell"] = "PowerShell",
        ["pwsh"] = "PowerShell",
        ["cmd"] = "Command Prompt",
        ["vlc"] = "VLC",
    };

    public static string? Display(string? processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return null;

        var name = processName.Trim();
        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            name = name[..^4];

        if (Known.TryGetValue(name, out var known)) return known;

        // All-caps process names shout in the list ("WINWORD"); soften them before capitalizing.
        if (name.All(c => !char.IsLetter(c) || char.IsUpper(c)))
            name = name.ToLowerInvariant();

        return char.ToUpperInvariant(name[0]) + name[1..];
    }
}
