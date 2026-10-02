using ClipShelf.Core.Theming;

namespace ClipShelf.App.Platform;

// Maps the tray icon style and the taskbar theme to the asset file. The taskbar theme can change
// while the app is running, so the name is resolved again on every change instead of once at
// startup.
public static class TrayIconFiles
{
    public static string FileName(TrayIconKind kind, bool taskbarIsLight) =>
        (kind, taskbarIsLight) switch
        {
            (TrayIconKind.Outline, true) => "tray-outline-light.ico",
            (TrayIconKind.Outline, false) => "tray-outline-dark.ico",
            (_, true) => "tray-light.ico",
            (_, false) => "tray-dark.ico",
        };
}
