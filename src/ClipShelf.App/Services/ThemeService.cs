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

        // Light and full black are opaque by definition; the backdrop setting applies otherwise.
        if (current.Theme is AppTheme.Light or AppTheme.White or AppTheme.Black)
        {
            window.SystemBackdrop = null;
            panel.Background = new SolidColorBrush(
                current.Theme == AppTheme.Black ? Colors.Black : Colors.White);
            return;
        }

        WindowSurface.ApplyBackdrop(window, panel, current.Backdrop);
    }
}
