using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace ClipShelf.App.Platform;

// Turns the clipboard's DIB into a PNG and builds the row thumbnail. WinRT imaging, so it can run
// off the UI thread.
public static class ImageClipCodec
{
    private const int ThumbnailWidth = 240;
    private const int ThumbnailHeight = 160;

    public static async Task<byte[]?> ToPngAsync(byte[] dib)
    {
        using var source = ToStream(WrapDibAsBmp(dib));
        var decoder = await BitmapDecoder.CreateAsync(source);
        var pixels = await decoder.GetSoftwareBitmapAsync();
        return await EncodePngAsync(pixels);
    }

    public static async Task<byte[]?> ThumbnailAsync(byte[] png)
    {
        using var source = ToStream(png);        var decoder = await BitmapDecoder.CreateAsync(source);

        var scale = Math.Min(
            1.0,
            Math.Min((double)ThumbnailWidth / decoder.PixelWidth, (double)ThumbnailHeight / decoder.PixelHeight));
        var transform = new BitmapTransform
        {
            ScaledWidth = Math.Max(1, (uint)Math.Round(decoder.PixelWidth * scale)),
            ScaledHeight = Math.Max(1, (uint)Math.Round(decoder.PixelHeight * scale)),
            InterpolationMode = BitmapInterpolationMode.Fant,
        };
        var pixels = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Premultiplied,
            transform,
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
        return await EncodePngAsync(pixels);
    }

    private static async Task<byte[]> EncodePngAsync(SoftwareBitmap pixels)
    {
        using var output = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
        encoder.SetSoftwareBitmap(pixels);
        await encoder.FlushAsync();
        return await ReadAllAsync(output);
    }

    // A 14-byte BITMAPFILEHEADER in front of the DIB makes it a .bmp the decoder accepts.
    private static byte[] WrapDibAsBmp(byte[] dib)
    {
        var file = new byte[14 + dib.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        BitConverter.TryWriteBytes(file.AsSpan(2), file.Length);
        BitConverter.TryWriteBytes(file.AsSpan(10), 14 + HeaderSize(dib));
        dib.CopyTo(file, 14);
        return file;
    }

    private static int HeaderSize(byte[] dib) =>
        dib.Length >= 4 && BitConverter.ToInt32(dib, 0) >= 12 ? BitConverter.ToInt32(dib, 0) : 40;

    private static IRandomAccessStream ToStream(byte[] bytes) =>
        new MemoryStream(bytes).AsRandomAccessStream();

    private static async Task<byte[]> ReadAllAsync(IRandomAccessStream stream)
    {
        using var encoded = stream.AsStreamForRead();
        var memory = new MemoryStream();
        await encoded.CopyToAsync(memory);
        return memory.ToArray();
    }
}
