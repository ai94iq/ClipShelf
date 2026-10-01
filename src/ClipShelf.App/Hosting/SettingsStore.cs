using System.Text.Json;

namespace ClipShelf.App.Hosting;

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public static AppSettings Load()
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

    public static void Save(AppSettings settings)
    {
        var temp = AppPaths.Settings + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
        File.Move(temp, AppPaths.Settings, overwrite: true);    // atomic replace
    }
}
