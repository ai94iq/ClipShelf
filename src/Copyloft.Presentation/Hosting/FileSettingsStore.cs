using System.Text.Json;

namespace Copyloft.Presentation.Hosting;

// settings.json under %LOCALAPPDATA%, written atomically. Another path reads a different install's
// settings without touching AppPaths (Program reads ClipShelf's while ClipShelf runs).
public sealed class FileSettingsStore(string? path = null) : ISettingsStore
{
    private string SettingsPath => path ?? AppPaths.Settings;

    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Load()
    {
        try
        {
            return File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), Options) ?? new()
                : new();
        }
        catch (JsonException)
        {
            return new();                                       // corrupt file → defaults
        }
    }

    public void Save(AppSettings settings)
    {
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, SettingsPath, overwrite: true);     // atomic replace
    }
}
