using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Core.Theming;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Services;

namespace ClipShelf.Desktop.Services;

// Maps the saved theme and accent onto Fluent. No accent means Fluent keeps the system accent.
// ponytail: Black renders as Dark; the dark pass (batch 8) adds its own surfaces.
public sealed class ThemeService : IThemeService
{
    private static readonly string[] AccentKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    public void Apply(AppSettings settings)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light or AppTheme.White => ThemeVariant.Light,
            AppTheme.Dark or AppTheme.Black => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        foreach (var key in AccentKeys) app.Resources.Remove(key);
        if (settings.Accent is null) return;

        var shades = AccentPalette.Shades(settings.Accent);
        string[] values =
            [shades.Base, shades.Light1, shades.Light2, shades.Light3, shades.Dark1, shades.Dark2, shades.Dark3];
        for (var i = 0; i < AccentKeys.Length; i++)
            app.Resources[AccentKeys[i]] = Color.Parse(values[i]);
    }
}
