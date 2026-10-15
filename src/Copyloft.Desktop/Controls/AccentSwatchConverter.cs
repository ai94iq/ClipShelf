using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Copyloft.Core.Theming;

namespace Copyloft.Desktop.Controls;

// Swatch colors: the preset hex, or the live platform accent for the "Windows accent" option
// (not the SystemAccentColor resource: a chosen preset already overrides that one).
public sealed class AccentSwatchConverter : IValueConverter
{
    public static AccentSwatchConverter Instance { get; } = new();

    private static readonly Color Fallback = Color.FromRgb(0x00, 0x78, 0xD4);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value is string hex ? ToColor(hex) : PlatformAccent();
        return new SolidColorBrush(color);
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
}
