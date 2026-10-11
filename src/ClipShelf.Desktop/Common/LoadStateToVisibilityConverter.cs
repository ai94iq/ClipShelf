using Avalonia.Data.Converters;
using ClipShelf.Presentation.Common;
using System;
using System.Globalization;
using System.Linq;

namespace ClipShelf.Desktop.Common;

// ConverterParameter: one state or a comma-separated list, e.g. 'Idle,Loading,Loaded'.
public sealed class LoadStateToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is LoadState state
            && parameter is string states
            && states.Split(',', StringSplitOptions.TrimEntries).Any(s => Enum.Parse<LoadState>(s) == state);

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}