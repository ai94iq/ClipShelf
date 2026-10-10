using Avalonia.Data.Converters;
using ClipShelf.Desktop.Resources;

namespace ClipShelf.Desktop.Common;

// Picks the pin glyph for a row: pinned items offer "unpin".
public sealed class PinIconConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? IconKind.PinOff : IconKind.Pin;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
