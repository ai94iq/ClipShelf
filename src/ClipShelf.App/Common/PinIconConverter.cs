using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Common;

// Picks the pin glyph for a row: pinned items offer "unpin".
public sealed partial class PinIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Resources.IconKind.PinOff : Resources.IconKind.Pin;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
