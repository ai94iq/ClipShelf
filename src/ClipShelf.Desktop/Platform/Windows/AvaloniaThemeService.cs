using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Core.Theming;

namespace ClipShelf.Desktop.Platform.Windows;

// Applies the theme variant and the accent color to the app resources. The Black theme is Dark
// plus black ground overrides, mirroring the WinUI build's approach; the accent hue is the user's
// setting and the world's blue stays the default.
public sealed class AvaloniaThemeService : IThemeService
{
    public void Apply(AppSettings settings)
    {
        if (Application.Current is not { } app) return;

        app.RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light or AppTheme.White => ThemeVariant.Light,
            AppTheme.Dark or AppTheme.Black => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        if (settings.Theme == AppTheme.Black)
        {
            app.Resources["GridPaperBrush"] = new SolidColorBrush(Color.Parse("#000000"));
            app.Resources["GridCellBrush"] = new SolidColorBrush(Color.Parse("#0B0B0C"));
            app.Resources["GridLineBrush"] = new SolidColorBrush(Color.Parse("#1A1B1E"));
        }
        else
        {
            // Root entries shadow the theme dictionaries, so visiting Black must not stain the
            // other themes afterwards.
            app.Resources.Remove("GridPaperBrush");
            app.Resources.Remove("GridCellBrush");
            app.Resources.Remove("GridLineBrush");
        }

        ApplyAccent(app, settings.Accent);
    }

    private static void ApplyAccent(Application app, string? accentHex)
    {
        if (accentHex is null) return;

        var (r, g, b) = AccentPalette.Parse(accentHex);
        var color = Color.FromRgb(r, g, b);
        app.Resources["GridAccentColor"] = color;
        app.Resources["GridAccentBrush"] = new SolidColorBrush(color);
        app.Resources["GridAccentPlaneBrush"] = new SolidColorBrush(color) { Opacity = 0.12 };
    }
}
