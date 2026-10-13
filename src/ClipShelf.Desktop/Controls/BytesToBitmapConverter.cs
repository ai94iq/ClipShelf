using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace ClipShelf.Desktop.Controls;

// Thumbnail bytes from the database to an image; bytes that do not decode show nothing.
public sealed class BytesToBitmapConverter : IValueConverter
{
    public static BytesToBitmapConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not byte[] { Length: > 0 } bytes) return null;
        try
        {
            using var stream = new MemoryStream(bytes);
            return new Bitmap(stream);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
