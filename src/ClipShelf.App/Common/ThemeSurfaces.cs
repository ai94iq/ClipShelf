using ClipShelf.App.Hosting;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Light and full black: recolor the shared surface brushes in place, so cards, panels and dialogs
// follow at once. Light keeps the Windows surface relationships — base and canvas share one gray,
// secondary sits a step darker, tertiary (popups, the undo bar) a clear step lighter, cards white —
// so the layers read apart. Originals are remembered per brush and restored for other themes.
internal static class ThemeSurfaces
{
    private const string CardKey = "CardBackgroundFillColorDefaultBrush";
    private const string SecondaryKey = "SolidBackgroundFillColorSecondaryBrush";
    private const string TertiaryKey = "SolidBackgroundFillColorTertiaryBrush";
    private const string LayerKey = "LayerFillColorDefaultBrush";

    public static readonly Windows.UI.Color LightCanvas = ColorHelper.FromArgb(255, 0xE6, 0xE6, 0xE6);
    public static readonly Windows.UI.Color LightCard = ColorHelper.FromArgb(255, 0xFF, 0xFF, 0xFF);
    private static readonly Windows.UI.Color LightSecondary = ColorHelper.FromArgb(255, 0xE1, 0xE1, 0xE1);
    private static readonly Windows.UI.Color LightTertiary = ColorHelper.FromArgb(255, 0xF2, 0xF2, 0xF2);
    private static readonly Windows.UI.Color LightLayer = ColorHelper.FromArgb(255, 0xEF, 0xEF, 0xEF);

    private static readonly string[] Keys =
    [
        "ApplicationPageBackgroundThemeBrush",
        "SolidBackgroundFillColorBaseBrush",
        SecondaryKey,
        TertiaryKey,
        CardKey,
        LayerKey,
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
                AppTheme.Light or AppTheme.White => key switch
                {
                    CardKey => LightCard,
                    SecondaryKey => LightSecondary,
                    TertiaryKey => LightTertiary,
                    LayerKey => LightLayer,
                    _ => LightCanvas,
                },
                AppTheme.Black => Colors.Black,
                _ => original,
            };
        }
    }
}
