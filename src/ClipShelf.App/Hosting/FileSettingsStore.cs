using System.Text.Json;

namespace ClipShelf.App.Hosting;

// settings.json under %LOCALAPPDATA%, written atomically.
public sealed class FileSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public AppSettings Load()
    {
        try
        {
            return File.Exists(AppPaths.Settings)
                ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(AppPaths.Settings), Options) ?? new()
                : new();
        }
        catch (JsonException)
        {
            return new();                                       // corrupt file → defaults
        }
    }

    public void Save(AppSettings settings)
    {
        var temp = AppPaths.Settings + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, AppPaths.Settings, overwrite: true);     // atomic replace
    }
}
