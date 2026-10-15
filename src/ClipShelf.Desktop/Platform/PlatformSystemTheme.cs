using Avalonia;
using Avalonia.Platform;

namespace ClipShelf.Desktop.Platform;

// The system theme as Avalonia reads it (the freedesktop color-scheme on Linux). Panels there
// follow it, so it stands in for the taskbar theme Windows reports separately.
public sealed class PlatformSystemTheme : ISystemTheme
{
    private readonly IPlatformSettings? _settings = Application.Current?.PlatformSettings;

    public PlatformSystemTheme()
    {
        if (_settings is not null)
            _settings.ColorValuesChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TaskbarIsLight => _settings?.GetColorValues().ThemeVariant != PlatformThemeVariant.Dark;

    public event EventHandler? Changed;
}
