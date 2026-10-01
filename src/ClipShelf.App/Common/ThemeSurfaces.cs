using ClipShelf.App.Hosting;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;

namespace ClipShelf.App.Common;

// Full black and full white themes: recolor the shared surface brushes in place, so cards, panels
// and dialogs follow at once. Originals are remembered per brush and restored for other themes.
internal static class ThemeSurfaces
{
    private static readonly string[] Keys =
    [
        "ApplicationPageBackgroundThemeBrush",
        "SolidBackgroundFillColorBaseBrush",
        "SolidBackgroundFillColorSecondaryBrush",
        "SolidBackgroundFillColorTertiaryBrush",
        "CardBackgroundFillColorDefaultBrush",
        "LayerFillColorDefaultBrush",
    ];

    private static readonly Dictionary<SolidColorBrush, Windows.UI.Color> Originals = [];

    public static void Apply(AppTheme theme)
    {
        var target = theme switch
        {
            AppTheme.Light or AppTheme.White => Colors.White,
            AppTheme.Black => Colors.Black,
            _ => (Windows.UI.Color?)null,
        };

        foreach (var key in Keys)
        {
            if (!Application.Current.Resources.TryGetValue(key, out var value) || value is not SolidColorBrush brush)
                continue;

            if (!Originals.TryGetValue(brush, out var original))
            {
                original = brush.Color;
                Originals[brush] = original;
            }

            brush.Color = target ?? original;
        }
    }
}
