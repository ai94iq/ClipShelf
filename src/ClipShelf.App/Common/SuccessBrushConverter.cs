using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Common;

// Status text color: the success (green) brush while a copy notice is showing, the secondary
// text brush otherwise.
public sealed partial class SuccessBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        Application.Current.Resources[value is true ? "SystemFillColorSuccessBrush" : "TextFillColorSecondaryBrush"];

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
