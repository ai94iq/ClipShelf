using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Services;

// Applies the theme and the backdrop to every attached window, live. The accent is fixed at
// startup (WinUI reads the accent resources when controls load), so changing it needs a restart.
public sealed class ThemeService(SettingsService settings) : IThemeService
{
    private readonly List<Window> _windows = [];

    public void Attach(Window window)
    {
        _windows.Add(window);
        ApplyTo(window);
    }

    public void Apply(AppSettings next)
    {
        foreach (var window in _windows.ToArray()) ApplyTo(window);
    }

    private void ApplyTo(Window window)
    {
        var current = settings.Current;

        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = current.Theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        if (window.Content is Panel panel) WindowSurface.ApplyBackdrop(window, panel, current.Backdrop);
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
