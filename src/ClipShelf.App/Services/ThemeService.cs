using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ClipShelf.App.Common;
using ClipShelf.App.Platform;
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
        ThemeSurfaces.Apply(next.Theme);
        foreach (var window in _windows.ToArray()) ApplyTo(window);
    }

    private void ApplyTo(Window window)
    {
        var current = settings.Current;

        if (window.Content is FrameworkElement root)
        {
            root.RequestedTheme = current.Theme switch
            {
                AppTheme.Light or AppTheme.White => ElementTheme.Light,
                AppTheme.Dark or AppTheme.Black => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }

        // The frame follows the theme setting, not the system: white themes need light caption
        // buttons even when Windows itself is dark.
        PanelFrame.SetDarkFrame(window, current.Theme switch
        {
            AppTheme.Light or AppTheme.White => false,
            AppTheme.Dark or AppTheme.Black => true,
            _ => (window.Content as FrameworkElement)?.ActualTheme == ElementTheme.Dark,
        });

        if (window.Content is not Panel panel) return;

        // Full black and the explicit light theme are opaque by definition; the backdrop setting
        // applies otherwise. Light sits on the same soft canvas as the shared surface brushes.
        if (current.Theme == AppTheme.Black)
        {
            window.SystemBackdrop = null;
            panel.Background = new SolidColorBrush(Colors.Black);
            return;
        }

        if (current.Theme is AppTheme.Light or AppTheme.White)
        {
            window.SystemBackdrop = null;
            panel.Background = new SolidColorBrush(ThemeSurfaces.LightCanvas);
            return;
        }

        WindowSurface.ApplyBackdrop(window, panel, current.Backdrop);
    }

    // Called once at startup, before any control loads: WinUI reads the accent resources when
    // controls are created, so the override cannot be applied later. Null keeps the Windows accent.
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
