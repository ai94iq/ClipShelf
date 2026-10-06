using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using ClipShelf.Core.Theming;

namespace ClipShelf.App.Common;

// Swatch colors: the preset hex, or the live Windows accent for the "Windows accent" option
// (UISettings, not the SystemAccentColor resource — a chosen preset already overrides that one).
// ConverterParameter "Check" returns the check color that reads on that swatch instead.
public sealed partial class AccentSwatchConverter : IValueConverter
{
    private static readonly Windows.UI.Color Fallback = ColorHelper.FromArgb(255, 0x00, 0x78, 0xD4);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value is string hex ? ToColor(hex) : WindowsAccent();
        return new SolidColorBrush(parameter is "Check" ? CheckColor(color) : color);
    }

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

    // Light swatches take a dark check and vice versa, like the Windows color picker.
    private static Windows.UI.Color CheckColor(Windows.UI.Color color)
    {
        var luminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255.0;
        return luminance > 0.25 ? Colors.Black : Colors.White;
    }
}
