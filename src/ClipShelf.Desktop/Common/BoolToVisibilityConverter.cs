using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ClipShelf.Desktop.Common;

// Turns a bool into bool for IsVisible with a converter.
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}