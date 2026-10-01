using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Common;

// Turns a bool from an x:Bind function into Visibility.
public sealed partial class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
