using System.Text.Json.Serialization;

namespace ClipShelf.App.Hosting;

// Saved as settings.json. Changing Language or digits takes effect after a restart.
public sealed record AppSettings
{
    public string Language { get; init; } = "ar-SA";          // "ar-SA" or "en-US"

    public bool UseHijri { get; init; }

    public bool ArabicIndicDigits { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; init; } = AppTheme.System;

    // Null = follow the Windows accent; otherwise one of AccentPresets (hex, e.g. "#0F6CBD").
    public string? Accent { get; init; }
}
