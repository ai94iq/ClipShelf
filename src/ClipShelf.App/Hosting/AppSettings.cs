using System.Text.Json.Serialization;

namespace ClipShelf.App.Hosting;

// Saved as settings.json. Changing Language takes effect after a restart.
public sealed record AppSettings
{
    public string Language { get; init; } = "en-US";

    public bool UseHijri { get; init; }

    public bool ArabicIndicDigits { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; init; } = AppTheme.System;

    // Null = follow the Windows accent; otherwise one of AccentPresets (hex, e.g. "#0F6CBD").
    public string? Accent { get; init; }

    // How many unpinned clips to keep; pinned clips are never pruned.
    public int MaxItems { get; init; } = 100;

    public bool RunAtStartup { get; init; }

    // When true, choosing an item also pastes it into the app that had focus.
    public bool PasteOnSelect { get; init; } = true;

    // When true, double-clicking the tray icon opens the full history window.
    public bool OpenHistoryOnDoubleClick { get; init; } = true;
}
