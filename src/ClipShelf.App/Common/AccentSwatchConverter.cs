using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Common;

// A swatch fill: the preset hex, or the live Windows accent for the "Windows accent" option
// (UISettings, not the SystemAccentColor resource — a chosen preset already overrides that one).
public sealed partial class AccentSwatchConverter : IValueConverter
{
    private static readonly Windows.UI.Color Fallback = ColorHelper.FromArgb(255, 0x00, 0x78, 0xD4);

    public object Convert(object value, Type targetType, object parameter, string language) =>
        new SolidColorBrush(value is string hex ? ToColor(hex) : WindowsAccent());

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();

    private static Windows.UI.Color ToColor(string hex)
    {
        var (r, g, b) = AccentPalette.Parse(hex);
        return ColorHelper.FromArgb(255, r, g, b);
    }

    private static Windows.UI.Color WindowsAccent()
    {
        try
        {
            return new UISettings().GetColorValue(UIColorType.Accent);
        }
        catch
        {
            return Fallback;    // headless or restricted contexts
        }
    }
}
