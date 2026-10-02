using System.IO;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace ClipShelf.App.Common;

// Turns a clip's thumbnail bytes into the image shown in its row.
public sealed partial class BytesToImageConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is not byte[] bytes || bytes.Length == 0) return null;

        try
        {
            var image = new BitmapImage();
            using var stream = new MemoryStream(bytes);
            image.SetSource(stream.AsRandomAccessStream());
            return image;
        }
        catch
        {
            // A damaged thumbnail should cost its row the image, not the whole view.
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
