using Microsoft.UI.Xaml.Data;

namespace ClipShelf.App.Common;

// Status text color: the critical (red) brush for errors, the secondary text brush otherwise.
public sealed partial class ErrorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        Application.Current.Resources[value is true ? "SystemFillColorCriticalBrush" : "TextFillColorSecondaryBrush"];

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
