using System.Runtime.Versioning;
namespace ClipShelf.Desktop.Platform.Windows;

// The notification area follows the system (taskbar) theme, not the app's theme, so the tray icon
// is chosen to stay visible on either a light or a dark taskbar. Changed fires from the registry
// watch and from the message pump's WM_SETTINGCHANGE fallback.
[SupportedOSPlatform("windows")]
internal sealed class SystemThemeService : ISystemTheme, IDisposable
{
    private readonly SystemThemeWatcher _watcher = new();

    public SystemThemeService()
    {
        _watcher.Changed += OnChanged;
        MessagePump.SystemThemeChanged += OnChanged;
    }

    public bool TaskbarIsLight
    {
        get
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                SystemThemeWatcher.PersonalizeKey);
            return key?.GetValue("SystemUsesLightTheme") is int value && value != 0;
        }
    }

    public event EventHandler? Changed;

    public void Dispose()
    {
        _watcher.Changed -= OnChanged;
        MessagePump.SystemThemeChanged -= OnChanged;
        _watcher.Dispose();
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);
}
