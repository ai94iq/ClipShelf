using System.Globalization;
using Avalonia.Data.Converters;

namespace ClipShelf.Desktop.Common;

// Shows a load-state block when the page state matches any of the comma-separated state names
// in the converter parameter.
public sealed class StateEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var current = value?.ToString();
        var names = parameter?.ToString()
            ?.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return current is not null && names?.Contains(current) == true;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
