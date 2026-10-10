using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ClipShelf.Core.Theming;

namespace ClipShelf.Desktop.Common;

// Swatch colors: the preset hex, or the live accent for the "Windows accent" option (null value,
// read from the applied theme resources). ConverterParameter "Check" returns the check color that
// reads on that swatch instead.
public sealed class AccentSwatchConverter : IValueConverter
{
    private static readonly Color Fallback = Color.Parse("#0057FF");

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value is string hex ? FromHex(hex) : CurrentAccent();
        return new SolidColorBrush(parameter is "Check" ? CheckColor(color) : color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color FromHex(string hex)
    {
        var (r, g, b) = AccentPalette.Parse(hex);
        return Color.FromRgb(r, g, b);
    }

    private static Color CurrentAccent()
    {
        if (Application.Current?.Resources.TryGetResource("GridAccentColor", null, out var resource) == true
            && resource is Color color)
            return color;
        return Fallback;
    }

    // Light swatches take a dark check and vice versa, like the Windows color picker.
    private static Color CheckColor(Color color)
    {
        var luminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255.0;
        return luminance > 0.25 ? Colors.Black : Colors.White;
    }
}
