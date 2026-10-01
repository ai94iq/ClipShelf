using Microsoft.UI;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Services;

// Theme applies live. The accent is set once at startup (WinUI reads accent resources when controls
// load), so changing it needs a restart. High Contrast is handled by Windows automatically.
public sealed class ThemeService(AppSettings startupSettings) : IThemeService
{
    private Window? _window;

    public void Attach(Window window)
    {
        _window = window;
        Apply(startupSettings);
    }

    public void Apply(AppSettings settings)
    {
        if (_window?.Content is not FrameworkElement root) return;
        root.RequestedTheme = settings.Theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            _ => ElementTheme.Default,
        };
    }

    // Call from the App constructor, after InitializeComponent. Null keeps the Windows accent.
    public static void ApplyAccentResources(ResourceDictionary resources, string? accentHex)
    {
        if (accentHex is null) return;
        var shades = AccentPalette.Shades(accentHex);
        resources["SystemAccentColor"] = ToColor(shades.Base);
        resources["SystemAccentColorLight1"] = ToColor(shades.Light1);
        resources["SystemAccentColorLight2"] = ToColor(shades.Light2);
        resources["SystemAccentColorLight3"] = ToColor(shades.Light3);
        resources["SystemAccentColorDark1"] = ToColor(shades.Dark1);
        resources["SystemAccentColorDark2"] = ToColor(shades.Dark2);
        resources["SystemAccentColorDark3"] = ToColor(shades.Dark3);
    }

    private static Windows.UI.Color ToColor(string hex)
    {
        var (r, g, b) = AccentPalette.Parse(hex);
        return ColorHelper.FromArgb(255, r, g, b);
    }
}
