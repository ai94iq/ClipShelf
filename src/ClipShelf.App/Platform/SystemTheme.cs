using Microsoft.Win32;

namespace ClipShelf.App.Platform;

// The notification area follows the system (taskbar) theme, not the app's theme, so the tray
// icon is chosen to stay visible on either a light or a dark taskbar.
internal static class SystemTheme
{
    internal const string PersonalizeKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool TaskbarIsLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
    }
}
