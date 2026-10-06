using ClipShelf.App.Hosting;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Light and full black: recolor the shared surface brushes in place, so cards, panels and dialogs
// follow at once. Light uses the Windows light values — a soft gray canvas with white cards —
// instead of a wall of pure white. Originals are remembered per brush and restored for other themes.
internal static class ThemeSurfaces
{
    private const string CardKey = "CardBackgroundFillColorDefaultBrush";

    // Windows' own light surfaces: the page canvas is a light gray, content cards stay white.
    public static readonly Windows.UI.Color LightCanvas = ColorHelper.FromArgb(255, 0xF3, 0xF3, 0xF3);
    public static readonly Windows.UI.Color LightCard = ColorHelper.FromArgb(255, 0xFF, 0xFF, 0xFF);

    private static readonly string[] Keys =
    [
        "ApplicationPageBackgroundThemeBrush",
        "SolidBackgroundFillColorBaseBrush",
        "SolidBackgroundFillColorSecondaryBrush",
        "SolidBackgroundFillColorTertiaryBrush",
        CardKey,
        "LayerFillColorDefaultBrush",
    ];

    private static readonly Dictionary<SolidColorBrush, Windows.UI.Color> Originals = [];

    public static void Apply(AppTheme theme)
    {
        foreach (var key in Keys)
        {
            if (!Application.Current.Resources.TryGetValue(key, out var value) || value is not SolidColorBrush brush)
                continue;

            if (!Originals.TryGetValue(brush, out var original))
            {
                original = brush.Color;
                Originals[brush] = original;
            }

            brush.Color = theme switch
            {
                AppTheme.Light or AppTheme.White => key == CardKey ? LightCard : LightCanvas,
                AppTheme.Black => Colors.Black,
                _ => original,
            };
        }
    }
}
