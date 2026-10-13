using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ClipShelf.Core.Theming;

namespace ClipShelf.Desktop.Controls;

// Swatch colors: the preset hex, or the live platform accent for the "Windows accent" option
// (not the SystemAccentColor resource: a chosen preset already overrides that one).
// ConverterParameter "Check" returns the check color that reads on that swatch instead.
public sealed class AccentSwatchConverter : IValueConverter
{
    public static AccentSwatchConverter Instance { get; } = new();

    private static readonly Color Fallback = Color.FromRgb(0x00, 0x78, 0xD4);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value is string hex ? ToColor(hex) : PlatformAccent();
        return new SolidColorBrush(parameter is "Check" ? CheckColor(color) : color);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Color ToColor(string hex)
    {
        var (r, g, b) = AccentPalette.Parse(hex);
        return Color.FromRgb(r, g, b);
    }

    private static Color PlatformAccent() =>
        Application.Current?.PlatformSettings?.GetColorValues().AccentColor1 ?? Fallback;

    // Light swatches take a dark check and vice versa, like the Windows color picker.
    private static Color CheckColor(Color color)
    {
        var luminance = ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255.0;
        return luminance > 0.25 ? Colors.Black : Colors.White;
    }
}
