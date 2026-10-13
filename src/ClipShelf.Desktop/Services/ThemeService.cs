using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using ClipShelf.Core.Theming;
using ClipShelf.Presentation.Hosting;
using ClipShelf.Presentation.Services;

namespace ClipShelf.Desktop.Services;

// Maps the saved theme and accent onto Fluent. No accent means Fluent keeps the system accent.
// Full black is its own variant on top of Dark: only the surfaces in Exceptions.axaml differ.
public sealed class ThemeService : IThemeService
{
    private static readonly string[] AccentKeys =
    [
        "SystemAccentColor",
        "SystemAccentColorLight1", "SystemAccentColorLight2", "SystemAccentColorLight3",
        "SystemAccentColorDark1", "SystemAccentColorDark2", "SystemAccentColorDark3",
    ];

    private const string AccentTextKey = "SystemControlForegroundAccentBrush";

    public static ThemeVariant Black { get; } = new("Black", ThemeVariant.Dark);

    // Text and glyphs drawn on an accent fill. Fluent paints them white, which vanishes on the
    // pastel presets, so they follow the accent's lightness instead.
    public static IReadOnlyList<string> OnAccentKeys { get; } =
    [
        "AccentButtonForeground", "AccentButtonForegroundPointerOver", "AccentButtonForegroundPressed",
        "ToggleSwitchKnobFillOn", "ToggleSwitchKnobFillOnPointerOver", "ToggleSwitchKnobFillOnPressed",
        "CheckBoxCheckGlyphForegroundChecked", "CheckBoxCheckGlyphForegroundCheckedPointerOver",
        "CheckBoxCheckGlyphForegroundCheckedPressed", "RadioButtonCheckGlyphFill",
    ];

    public void Apply(AppSettings settings)
    {
        var app = Application.Current!;
        app.RequestedThemeVariant = settings.Theme switch
        {
            AppTheme.Light or AppTheme.White => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Black => Black,
            _ => ThemeVariant.Default,
        };

        WindowSurface.Apply(settings);

        foreach (var key in AccentKeys.Concat(OnAccentKeys)) app.Resources.Remove(key);
        LightDictionary(app).Remove(AccentTextKey);
        if (settings.Accent is null) return;

        var shades = AccentPalette.Shades(settings.Accent);
        string[] values =
            [shades.Base, shades.Light1, shades.Light2, shades.Light3, shades.Dark1, shades.Dark2, shades.Dark3];
        for (var i = 0; i < AccentKeys.Length; i++)
            app.Resources[AccentKeys[i]] = Color.Parse(values[i]);

        var onAccent = new SolidColorBrush(OnAccent(Color.Parse(shades.Base)));
        foreach (var key in OnAccentKeys) app.Resources[key] = onAccent;
        LightDictionary(app)[AccentTextKey] = new SolidColorBrush(Color.Parse(shades.Dark2));
    }

    // Same rule as the swatch check: light accents take black, dark ones white.
    private static Color OnAccent(Color accent)
    {
        var luminance = ((0.2126 * accent.R) + (0.7152 * accent.G) + (0.0722 * accent.B)) / 255.0;
        return luminance > 0.25 ? Colors.Black : Colors.White;
    }

    private static IResourceDictionary LightDictionary(Application app)
    {
        if (!app.Resources.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out var light))
        {
            light = new ResourceDictionary();
            app.Resources.ThemeDictionaries[ThemeVariant.Light] = light;
        }

        return (IResourceDictionary)light;
    }
}
