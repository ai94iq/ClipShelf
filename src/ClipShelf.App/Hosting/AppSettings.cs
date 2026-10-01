using System.Text.Json.Serialization;
using ClipShelf.Core.Input;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Hosting;

// Saved as settings.json. Changing Language takes effect after a restart.
public sealed record AppSettings
{
    public static HotkeyGesture DefaultHotkey { get; } =
        new(HotkeyModifiers.Win | HotkeyModifiers.Shift, 0x56); // V

    public string Language { get; init; } = "en-US";

    public bool UseHijri { get; init; }

    public bool ArabicIndicDigits { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<AppTheme>))]
    public AppTheme Theme { get; init; } = AppTheme.System;

    // Window backdrop, from WinUI 3.
    [JsonConverter(typeof(JsonStringEnumConverter<BackdropKind>))]
    public BackdropKind Backdrop { get; init; } = BackdropKind.Mica;

    // Null = follow the Windows accent; otherwise one of AccentPresets (hex, e.g. "#0F6CBD").
    public string? Accent { get; init; }

    // How many uncategorized clips to keep; pinned and categorized clips are never pruned.
    public int MaxItems { get; init; } = 100;

    // Days to keep uncategorized clips; 0 keeps them forever. Pinned and categorized clips are never pruned.
    public int RetentionDays { get; init; }

    // When true, unpinned clips are cleared when the user signs out or shuts down.
    public bool ClearOnSignOut { get; init; }

    public bool RunAtStartup { get; init; }

    // Global shortcut that opens the flyout; at least one modifier plus one key.
    [JsonConverter(typeof(HotkeyGestureJsonConverter))]
    public HotkeyGesture Hotkey { get; init; } = DefaultHotkey;

    // When true, choosing an item also pastes it into the app that had focus.
    public bool PasteOnSelect { get; init; } = true;

    // When true, double-clicking the tray icon opens the full history window.
    public bool OpenHistoryOnDoubleClick { get; init; } = true;

    // PBKDF2 hash of the export password; null means exporting does not ask for one.
    public string? ExportPasswordHash { get; init; }

    [JsonConverter(typeof(JsonStringEnumConverter<TrayIconKind>))]
    public TrayIconKind TrayIcon { get; init; } = TrayIconKind.Filled;

    public bool ShowTrayIcon { get; init; } = true;
}
